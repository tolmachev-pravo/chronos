using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Application.Users.Commands;
using Chronos.Application.Users.Dto;
using System;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Profile
{
    /// <summary>
    /// Working day of the signed-in user (issue #241). The values used to be asked for in
    /// the worklog filter on every search; here they are set once and stored per user.
    /// </summary>
    public partial class ProfileDialog : ComponentBase
    {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public string Username { get; set; } = string.Empty;
        [Parameter] public string Avatar { get; set; } = string.Empty;
        [Parameter] public UserSettingsDto Settings { get; set; } = UserSettingsDto.Default;

        [Inject] private IMediator Mediator { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private TimeSpan? _workingStartTime;
        private TimeSpan? _workingEndTime;
        private TimeSpan? _lunchTime;

        protected override void OnParametersSet()
        {
            _workingStartTime = Settings.WorkingStartTime;
            _workingEndTime = Settings.WorkingEndTime;
            _lunchTime = Settings.LunchTime;
        }

        /// <summary>
        /// Mirrors UpsertUserSettingsValidator so the dialog says what is wrong instead of
        /// letting the command fail.
        /// </summary>
        private string ValidationError
        {
            get
            {
                if (_workingStartTime is null || _workingEndTime is null || _lunchTime is null)
                    return "Заполните все три поля";
                if (_workingEndTime <= _workingStartTime)
                    return "Время окончания работы должно быть больше времени начала";
                if (_lunchTime >= _workingEndTime - _workingStartTime)
                    return "Обед должен быть короче рабочего дня";
                return null;
            }
        }

        private TimeSpan WorkingTime =>
            (_workingEndTime ?? TimeSpan.Zero) - (_workingStartTime ?? TimeSpan.Zero) - (_lunchTime ?? TimeSpan.Zero);

        private string WorkingTimeDisplay => WorkingTime.Minutes == 0
            ? $"{(int)WorkingTime.TotalHours} ч"
            : $"{(int)WorkingTime.TotalHours} ч {WorkingTime.Minutes} мин";

        private async Task Save()
        {
            if (ValidationError is not null)
                return;

            var settings = new UserSettingsDto(
                _workingStartTime.Value,
                _workingEndTime.Value,
                _lunchTime.Value);

            await Mediator.Send(new UpsertUserSettings.Command(Username, settings));
            Snackbar.Add("Настройки профиля сохранены", Severity.Success);
            MudDialog.Close(DialogResult.Ok(settings));
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
