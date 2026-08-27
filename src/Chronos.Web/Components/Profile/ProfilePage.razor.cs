using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using Chronos.Infrastructure.Jira;
using Chronos.Application.Authentication;
using Chronos.Application.Storage;
using Chronos.Application.Users.Commands;
using Chronos.Application.Users.Dto;
using Chronos.Application.Users.Queries;
using Chronos.Domain.Models.Users;
using Chronos.Web.Shared;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Profile
{
    /// <summary>
    /// Personal settings of the signed-in user (issue #241). The working day used to be
    /// asked for in the worklog filter on every search; here it is set once and stored
    /// per user, next to the rest of the account.
    /// </summary>
    public partial class ProfilePage : ComponentBase
    {
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }

        /// <summary>
        /// The theme provider is rendered by the layout, so the switch on this page asks
        /// the layout to apply and store the change.
        /// </summary>
        [CascadingParameter] public MainLayout Layout { get; set; }

        [Inject] private IMediator Mediator { get; set; }
        [Inject] private IIdentityService IdentityService { get; set; }
        [Inject] private IStorage<string, UserProfile> UserProfileStorage { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IOptions<JiraConfiguration> JiraConfiguration { get; set; }

        private string Username { get; set; } = string.Empty;
        private string _avatar = string.Empty;
        private string _displayName;
        private string _email;
        private string _timeZoneId;
        private UserSettingsDto _savedSettings = UserSettingsDto.Default;
        private bool _signedInWithToken;

        private TimeSpan? _workingStartTime;
        private TimeSpan? _workingEndTime;
        private TimeSpan? _lunchTime;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var user = await IdentityService.GetCurrentUserAsync();
                Username = user?.Username ?? string.Empty;
                _signedInWithToken = !string.IsNullOrEmpty(user?.PersonalAccessToken);
                ApplySettings(await Mediator.Send(new GetUserSettings.Query(Username)));
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        /// <summary>
        /// The Jira account details come from local storage, which needs JS interop —
        /// available only after the first render. A profile cached before the name and the
        /// email were stored is refreshed from Jira once, so old sessions fill in too.
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            try
            {
                var profile = await UserProfileStorage.GetValueAsync(Username);
                if (profile != null && profile.DisplayName == null)
                {
                    await UserProfileStorage.ForceInitAsync(Username);
                    profile = await UserProfileStorage.GetValueAsync(Username);
                }

                ApplyProfile(profile);
                StateHasChanged();
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        private void ApplyProfile(UserProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            _avatar = profile.Avatar;
            _displayName = profile.DisplayName;
            _email = profile.Email;
            _timeZoneId = profile.TimeZoneId;
        }

        /// <summary>
        /// Time zone as Jira reports it, with the offset it resolves to right now. An id the
        /// runtime does not know is shown as it is rather than breaking the page.
        /// </summary>
        private string TimeZoneDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(_timeZoneId))
                {
                    return null;
                }

                try
                {
                    var offset = TimeZoneConverter.TZConvert.GetTimeZoneInfo(_timeZoneId)
                        .GetUtcOffset(DateTime.UtcNow);
                    var sign = offset < TimeSpan.Zero ? "-" : "+";
                    return $"{_timeZoneId} (UTC{sign}{offset.Duration():hh\\:mm})";
                }
                catch (Exception)
                {
                    return _timeZoneId;
                }
            }
        }

        private string SignInMethod => _signedInWithToken
            ? "Персональный токен (PAT)"
            : "Логин и пароль";

        private string JiraUrl => JiraConfiguration?.Value?.Url;

        /// <summary>
        /// The user's own page in Jira — where everything on the account card is edited.
        /// </summary>
        private string JiraProfileUrl => string.IsNullOrEmpty(JiraUrl)
            ? null
            : $"{JiraUrl.TrimEnd('/')}/secure/ViewProfile.jspa";

        private static string Display(string value) =>
            string.IsNullOrWhiteSpace(value) ? "—" : value;

        private bool IsDarkMode => Layout?.IsDarkMode ?? false;

        private async Task ToggleThemeAsync(bool value)
        {
            try
            {
                await Layout.ToggleThemeAsync(value);
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        private void ApplySettings(UserSettingsDto settings)
        {
            _savedSettings = settings;
            _workingStartTime = settings.WorkingStartTime;
            _workingEndTime = settings.WorkingEndTime;
            _lunchTime = settings.LunchTime;
        }

        /// <summary>
        /// Whether the pickers hold something other than what is stored — the save button
        /// stays quiet until there is a change to save.
        /// </summary>
        private bool IsDirty =>
            _workingStartTime != _savedSettings.WorkingStartTime
            || _workingEndTime != _savedSettings.WorkingEndTime
            || _lunchTime != _savedSettings.LunchTime;

        /// <summary>
        /// Mirrors UpsertUserSettingsValidator so the page says what is wrong instead of
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

        private TimeSpan DayLength =>
            (_workingEndTime ?? TimeSpan.Zero) - (_workingStartTime ?? TimeSpan.Zero);

        private string WorkingTimeDisplay => Duration(WorkingTime);

        private string DurationDisplay => Duration(_lunchTime ?? TimeSpan.Zero);

        private static string Duration(TimeSpan value) => value.Minutes == 0
            ? $"{(int)value.TotalHours} ч"
            : value.TotalHours < 1
                ? $"{value.Minutes} мин"
                : $"{(int)value.TotalHours} ч {value.Minutes} мин";

        private static string Time(TimeSpan value) => value.ToString(@"hh\:mm");

        /// <summary>
        /// Share of the day a segment takes, for the working day bar.
        /// </summary>
        private string Percent(TimeSpan part) => DayLength > TimeSpan.Zero
            ? (part / DayLength * 100).ToString("0.##", CultureInfo.InvariantCulture)
            : "0";

        private async Task SaveWorkingDayAsync()
        {
            if (ValidationError is not null)
            {
                return;
            }

            try
            {
                var settings = new UserSettingsDto(
                    _workingStartTime.Value, _workingEndTime.Value, _lunchTime.Value);
                await Mediator.Send(new UpsertUserSettings.Command(Username, settings));
                _savedSettings = settings;
                Snackbar.Add("Рабочий день сохранён", Severity.Success);
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }
    }
}
