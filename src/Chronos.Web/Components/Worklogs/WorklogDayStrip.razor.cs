using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
                        ? Cell.Pending(date, WorkingDayFormat.Norm(Settings), isLoaded: days is not null)
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
            DateTime Date,
            bool IsLoaded,
            bool IsWeekend,
            TimeSpan Logged,
            TimeSpan Norm,
            int Suggestions)
        {
            public static Cell Pending(DateTime date, TimeSpan norm, bool isLoaded) =>
                new(date, isLoaded, IsWeekendDay(date), TimeSpan.Zero, IsWeekendDay(date) ? TimeSpan.Zero : norm, 0);

            public static Cell From(WorkingDay day) =>
                new(day.Date.Date, true, day.IsWeekend, day.ActualWorklogTimeSpent,
                    day.IsWeekend ? TimeSpan.Zero : day.Settings.WorkingTime, day.RawEstimatedWorklogCount);

            private static bool IsWeekendDay(DateTime date) =>
                date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            public bool IsClosed => IsLoaded && Norm > TimeSpan.Zero && Logged >= Norm;

            /// <summary>A working day that is over and still short of its norm.</summary>
            public bool IsShort => IsLoaded && !IsWeekend && Logged < Norm && Date < DateTime.Today;

            public int Percent => Norm > TimeSpan.Zero
                ? (int)Math.Min(100, Math.Round(Logged / Norm * 100))
                : 0;

            public string Hours
            {
                get
                {
                    if (IsWeekend)
                        return Logged > TimeSpan.Zero ? $"{WorklogDayStrip.Hours(Logged)} ч" : "выходной";
                    var logged = IsLoaded ? WorklogDayStrip.Hours(Logged) : "?";
                    return $"{logged} / {WorklogDayStrip.Hours(Norm)} ч";
                }
            }

            public string Title => Date.ToString("dddd, d MMMM", Russian);

            public string Class
            {
                get
                {
                    var classes = new List<string> { "chr-day-strip__day" };
                    if (IsWeekend) classes.Add("chr-day-strip__day--weekend");
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
