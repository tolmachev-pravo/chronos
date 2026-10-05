using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Application.Authentication;
using Chronos.Application.Calendar.Commands;
using Chronos.Application.Calendar.Dto;
using Chronos.Application.Calendar.Queries;
using Chronos.Domain.Entities.Calendar;
using Chronos.Web.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Profile
{
    /// <summary>
    /// The list of the user's absences, with adding and removing. See issue #310.
    /// </summary>
    public partial class UserAbsencesCard : ComponentBase
    {
        private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }

        [Inject] private IMediator Mediator { get; set; }
        [Inject] private IIdentityService IdentityService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        private string _username;
        private IReadOnlyList<UserAbsenceDto> _absences = Array.Empty<UserAbsenceDto>();

        private DateRange _range;
        private AbsenceKind _kind = AbsenceKind.Vacation;
        private string _comment;
        private bool _saving;

        private bool CanAdd => !_saving
            && !string.IsNullOrEmpty(_username)
            && _range?.Start is not null
            && _range.End is not null;

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

        private async Task ReadAsync() =>
            _absences = await Mediator.Send(new GetUserAbsences.Query(_username));

        private async Task AddAsync()
        {
            if (!CanAdd)
                return;

            _saving = true;
            try
            {
                await Mediator.Send(new AddUserAbsence.Command(
                    _username, _range.Start!.Value, _range.End!.Value, _kind, _comment));
                Snackbar.Add($"{Worklogs.WorkingDayLabel.Absence(_kind)} добавлен", Severity.Success);
                _range = null;
                _comment = null;
                await ReadAsync();
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
            finally
            {
                _saving = false;
            }
        }

        private async Task DeleteAsync(UserAbsenceDto absence)
        {
            try
            {
                await Mediator.Send(new DeleteUserAbsence.Command(_username, absence.Id));
                await ReadAsync();
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        /// <summary>«14 июля — 27 июля 2026 · 14 дн.», or a single date for a one-day absence.</summary>
        private static string Range(UserAbsenceDto absence)
        {
            if (absence.StartDate == absence.EndDate)
                return absence.StartDate.ToString("d MMMM yyyy", Russian);

            var start = absence.StartDate.Year == absence.EndDate.Year
                ? absence.StartDate.ToString("d MMMM", Russian)
                : absence.StartDate.ToString("d MMMM yyyy", Russian);
            return $"{start} — {absence.EndDate.ToString("d MMMM yyyy", Russian)} · {absence.Days} дн.";
        }
    }
}
