using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Chronos.Application.Authentication;
using Chronos.Application.Calendar;
using Chronos.Application.Calendar.Commands;
using Chronos.Application.Calendar.Dto;
using Chronos.Application.Calendar.Queries;
using Chronos.Domain.Entities.Calendar;
using Chronos.Web.Components.Worklogs;
using Chronos.Web.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Profile.Absences
{
    /// <summary>
    /// The user's absences laid over the whole year, like a wall calendar with holidays
    /// already marked (issue #310). Dragging across days opens a small card to say what
    /// they are; a click on an absence opens the same card to change or remove it.
    /// </summary>
    public partial class AbsenceYear : ComponentBase, IAsyncDisposable
    {
        private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
        private static readonly string[] Weekdays = { "пн", "вт", "ср", "чт", "пт", "сб", "вс" };

        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }

        /// <summary>Raised whenever the absences change, so the tab can count the ones ahead.</summary>
        [Parameter] public EventCallback<IReadOnlyList<UserAbsenceDto>> OnChanged { get; set; }

        [Inject] private IMediator Mediator { get; set; }
        [Inject] private IIdentityService IdentityService { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        private ElementReference _grid;
        private ElementReference _composerElement;
        private DotNetObjectReference<AbsenceYear> _self;
        private bool _attached;
        private bool _placeComposer;

        private string _username;
        private int _year = DateTime.Today.Year;
        private AbsencePlan _plan = new(null);
        private IReadOnlyList<UserAbsenceDto> _absences = Array.Empty<UserAbsenceDto>();
        private Composer _composer;

        private DateTime Today => DateTime.Today;

        private AbsencePlan.Balance Balance => _plan.BalanceOf(_year, _absences, Today);

        private IEnumerable<UserAbsenceDto> YearAbsences => _absences
            .Where(absence => absence.StartDate.Year <= _year && absence.EndDate.Year >= _year)
            .OrderBy(absence => absence.StartDate);

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var user = await IdentityService.GetCurrentUserAsync();
                _username = user?.Username;
                await ReadAsync();
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_attached)
            {
                _self = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("chronosAbsenceYear.attach", _grid, _self);
                _attached = true;
            }

            if (_placeComposer && _composer is not null)
            {
                _placeComposer = false;
                await JS.InvokeVoidAsync("chronosAbsenceYear.place", _composerElement, _composer.X, _composer.Y);
            }
        }

        private async Task ReadAsync()
        {
            var first = new DateTime(_year, 1, 1);
            var calendar = await Mediator.Send(new GetWorkingCalendar.Query(
                _username, first, first.AddYears(1).AddDays(-1), IncludeAbsences: false));
            _plan = new AbsencePlan(calendar);
            _absences = await Mediator.Send(new GetUserAbsences.Query(_username));
        }

        private async Task ShiftYearAsync(int years)
        {
            _year += years;
            await CloseComposerAsync();
            try
            {
                await ReadAsync();
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        /// <summary>A drag ended: a single click on an absence opens it, anything else is a new one.</summary>
        [JSInvokable]
        public Task OnDaysSelected(string from, string to, double x, double y)
        {
            var start = DateTime.ParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var end = DateTime.ParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            var existing = start == end ? AbsenceOn(start) : null;
            _composer = existing is not null
                ? Composer.Edit(existing, x, y)
                : Composer.Create(start, end, x, y);
            _placeComposer = true;
            StateHasChanged();
            return Task.CompletedTask;
        }

        private void OpenFromList(UserAbsenceDto absence, MouseEventArgs args)
        {
            _composer = Composer.Edit(absence, args.ClientX, args.ClientY);
            _placeComposer = true;
        }

        private async Task CloseComposerAsync()
        {
            _composer = null;
            await JS.InvokeVoidAsync("chronosAbsenceYear.clear", _grid);
        }

        private async Task SaveAsync()
        {
            if (_composer is not { CanSave: true })
                return;

            var composer = _composer;
            if (Overlapping(composer) is { } other)
            {
                Snackbar.Add($"Даты пересекаются: {Describe(other)}", Severity.Warning);
                return;
            }

            try
            {
                if (composer.Existing is null)
                {
                    await Mediator.Send(new AddUserAbsence.Command(
                        _username, composer.Start.Value, composer.End.Value, composer.Kind, composer.Comment));
                    Snackbar.Add($"{WorkingDayLabel.Absence(composer.Kind)} добавлен", Severity.Success);
                }
                else
                {
                    await Mediator.Send(new UpdateUserAbsence.Command(
                        _username, composer.Existing.Id, composer.Start.Value, composer.End.Value, composer.Kind, composer.Comment));
                }

                await AfterChangeAsync();
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        private async Task DeleteAsync(UserAbsenceDto absence)
        {
            try
            {
                await Mediator.Send(new DeleteUserAbsence.Command(_username, absence.Id));
                await AfterChangeAsync();
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        private async Task AfterChangeAsync()
        {
            await CloseComposerAsync();
            _absences = await Mediator.Send(new GetUserAbsences.Query(_username));
            await OnChanged.InvokeAsync(_absences);
        }

        private UserAbsenceDto AbsenceOn(DateTime date) =>
            _absences.FirstOrDefault(absence => absence.StartDate <= date && date <= absence.EndDate);

        private UserAbsenceDto Overlapping(Composer composer) =>
            composer.Start is null || composer.End is null
                ? null
                : _absences.FirstOrDefault(absence => absence.Id != composer.Existing?.Id
                    && absence.StartDate <= composer.End.Value.Date
                    && absence.EndDate >= composer.Start.Value.Date);

        private string DayClass(DateTime date)
        {
            var day = _plan.DayOf(date);
            var absence = AbsenceOn(date);
            var classes = new List<string> { "abs-day" };
            if (!day.IsWorking) classes.Add("abs-day--off");
            if (day.Kind == WorkingDayKind.Holiday) classes.Add("abs-day--holiday");
            if (day.Kind == WorkingDayKind.ShortDay) classes.Add("abs-day--short");
            if (date == Today) classes.Add("abs-day--today");
            if (absence is not null)
            {
                classes.Add($"abs-day--{KindClass(absence.Kind)}");
                if (absence.StartDate == date) classes.Add("abs-day--first");
                if (absence.EndDate == date) classes.Add("abs-day--last");
            }
            return string.Join(' ', classes);
        }

        private string DayTitle(DateTime date)
        {
            var parts = new List<string> { date.ToString("d MMMM, dddd", Russian) };
            var day = _plan.DayOf(date);
            if (WorkingDayLabel.Describe(day) is { } kind) parts.Add(kind);
            if (AbsenceOn(date) is { } absence) parts.Add(Describe(absence));
            return string.Join(" — ", parts);
        }

        private static string KindClass(AbsenceKind kind) => kind switch
        {
            AbsenceKind.SickLeave => "sick",
            AbsenceKind.DayOff => "dayoff",
            _ => "vacation"
        };

        private static string Describe(UserAbsenceDto absence) =>
            $"{WorkingDayLabel.Absence(absence.Kind)}{(string.IsNullOrEmpty(absence.Comment) ? null : " · " + absence.Comment)}";

        private static string MonthName(int year, int month)
        {
            var name = Russian.DateTimeFormat.GetMonthName(month);
            return char.ToUpper(name[0], Russian) + name[1..];
        }

        private static int LeadingBlanks(DateTime first) => ((int)first.DayOfWeek + 6) % 7;

        public static string Range(DateTime start, DateTime end)
        {
            if (start == end)
                return start.ToString("d MMMM", Russian);
            return start.Month == end.Month && start.Year == end.Year
                ? $"{start.Day}–{end.ToString("d MMMM", Russian)}"
                : $"{start.ToString("d MMMM", Russian)} — {end.ToString("d MMMM", Russian)}";
        }

        public static string Days(int count)
        {
            var tens = count % 100;
            var units = count % 10;
            if (tens is >= 11 and <= 14 || units is 0 or >= 5) return $"{count} дней";
            return units == 1 ? $"{count} день" : $"{count} дня";
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_attached)
                    await JS.InvokeVoidAsync("chronosAbsenceYear.detach", _grid);
            }
            catch (JSDisconnectedException)
            {
                // The circuit is gone, and the page with it.
            }
            _self?.Dispose();
        }

        /// <summary>The card a selection opens: what the days are, and the dates themselves.</summary>
        private sealed class Composer
        {
            public UserAbsenceDto Existing { get; private init; }
            public DateTime? Start { get; set; }
            public DateTime? End { get; set; }
            public AbsenceKind Kind { get; set; } = AbsenceKind.Vacation;
            public string Comment { get; set; }
            public double X { get; private init; }
            public double Y { get; private init; }

            public bool CanSave => Start is not null && End is not null && End >= Start;

            public static Composer Create(DateTime start, DateTime end, double x, double y) =>
                new() { Start = start, End = end, X = x, Y = y };

            public static Composer Edit(UserAbsenceDto absence, double x, double y) => new()
            {
                Existing = absence,
                Start = absence.StartDate,
                End = absence.EndDate,
                Kind = absence.Kind,
                Comment = absence.Comment,
                X = x,
                Y = y
            };
        }
    }
}
