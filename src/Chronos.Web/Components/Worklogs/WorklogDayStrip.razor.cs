using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Chronos.Application.Calendar;
using Chronos.Application.Users.Dto;
using Chronos.Application.Worklogs.Dto;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogDayStrip : ComponentBase
    {
        private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

        [Parameter] public WorklogPeriod Period { get; set; }

        /// <summary>The days read for <see cref="Period"/>; null while it is not on screen.</summary>
        [Parameter] public IEnumerable<WorkingDay> Days { get; set; }

        [Parameter] public UserSettingsDto Settings { get; set; } = UserSettingsDto.Default;

        /// <summary>
        /// What each day of <see cref="Period"/> is — so a holiday or a vacation is not drawn
        /// as a working day before the period is read. Days it does not hold are told by
        /// their weekday. See issue #310.
        /// </summary>
        [Parameter] public IReadOnlyDictionary<DateTime, WorkingCalendarDay> Calendar { get; set; }

        [Inject] private IJSRuntime JS { get; set; }

        /// <summary>The id of a day's row in the list, for the strip to scroll to.</summary>
        public static string AnchorOf(DateTime date) => $"chr-day-{date:yyyy-MM-dd}";

        private static readonly string[] Weekdays = { "пн", "вт", "ср", "чт", "пт", "сб", "вс" };

        /// <summary>
        /// A week reads as a list; anything longer would run the column out of height and
        /// is laid out as a calendar instead.
        /// </summary>
        private bool IsCalendar => (Period.End - Period.Start).Days >= 7;

        /// <summary>Empty cells before the first day, so it sits under its own weekday.</summary>
        private int LeadingBlanks => ((int)Period.Start.DayOfWeek + 6) % 7;

        private IEnumerable<Cell> Cells
        {
            get
            {
                var days = Days?.ToDictionary(day => day.Date.Date);
                for (var date = Period.Start; date <= Period.End; date = date.AddDays(1))
                {
                    var day = days?.GetValueOrDefault(date);
                    yield return day is null
                        ? Cell.Pending(
                            Calendar?.GetValueOrDefault(date) ?? WorkingCalendarDay.ByWeekday(date),
                            WorkingDayFormat.Norm(Settings),
                            isLoaded: days is not null)
                        : Cell.From(day);
                }
            }
        }

        private async Task ScrollToAsync(DateTime date)
        {
            try
            {
                await JS.InvokeVoidAsync("chronosScroll.toElement", AnchorOf(date));
            }
            catch (JSException)
            {
                // Scrolling is a convenience; the list is still there to scroll by hand.
            }
        }

        private static string Suggestions(int count)
        {
            var tens = count % 100;
            var units = count % 10;
            if (tens is >= 11 and <= 14 || units is 0 or >= 5)
                return "предложений";
            return units == 1 ? "предложение" : "предложения";
        }

        private static string Hours(TimeSpan value) =>
            value.TotalHours == Math.Floor(value.TotalHours) ? $"{(int)value.TotalHours}" : $"{value.TotalHours:0.#}";

        private sealed record Cell(
            WorkingCalendarDay Day,
            bool IsLoaded,
            TimeSpan Logged,
            TimeSpan Norm,
            int Suggestions)
        {
            public static Cell Pending(WorkingCalendarDay day, TimeSpan workingTime, bool isLoaded) =>
                new(day, isLoaded, TimeSpan.Zero, day.NormOf(workingTime), 0);

            public static Cell From(WorkingDay day) =>
                new(day.Calendar, true, day.ActualWorklogTimeSpent, day.Norm, day.OpenSuggestionCount);

            public DateTime Date => Day.Date;

            /// <summary>A weekend, a holiday or an absence: no norm, so no bar and no shortfall.</summary>
            public bool IsDayOff => !Day.IsWorking;

            public bool IsClosed => IsLoaded && Norm > TimeSpan.Zero && Logged >= Norm;

            /// <summary>A working day that is over and still short of its norm.</summary>
            public bool IsShort => IsLoaded && !IsDayOff && Logged < Norm && Date < DateTime.Today;

            public int Percent => Norm > TimeSpan.Zero
                ? (int)Math.Min(100, Math.Round(Logged / Norm * 100))
                : 0;

            public string Hours
            {
                get
                {
                    if (IsDayOff)
                        return Logged > TimeSpan.Zero ? $"{WorklogDayStrip.Hours(Logged)} ч" : WorkingDayLabel.Short(Day);
                    var logged = IsLoaded ? WorklogDayStrip.Hours(Logged) : "?";
                    return $"{logged} / {WorklogDayStrip.Hours(Norm)} ч";
                }
            }

            public string Title => WorkingDayLabel.Describe(Day) is { } kind
                ? $"{Date.ToString("dddd, d MMMM", Russian)} — {kind}"
                : Date.ToString("dddd, d MMMM", Russian);

            public string Class
            {
                get
                {
                    var classes = new List<string> { "chr-day-strip__day" };
                    if (IsDayOff) classes.Add("chr-day-strip__day--weekend");
                    if (Day.Kind == WorkingDayKind.Holiday) classes.Add("chr-day-strip__day--holiday");
                    if (Day.Kind == WorkingDayKind.Absence) classes.Add("chr-day-strip__day--absence");
                    if (!IsLoaded) classes.Add("chr-day-strip__day--pending");
                    if (IsClosed) classes.Add("chr-day-strip__day--closed");
                    if (IsShort) classes.Add("chr-day-strip__day--short");
                    if (Date == DateTime.Today) classes.Add("chr-day-strip__day--today");
                    return string.Join(' ', classes);
                }
            }
        }
    }
}
