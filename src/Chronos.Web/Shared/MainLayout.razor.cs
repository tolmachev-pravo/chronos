using MediatR;
using Microsoft.AspNetCore.Components;
using Chronos.Application.Authentication;
using Chronos.Application.Extensions.Jira.Commands;
using Chronos.Application.Storage;
using Chronos.Application.Users;
using Chronos.Application.Users.Commands;
using Chronos.Application.Users.Dto;
using Chronos.Application.Users.Queries;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Users;
using Chronos.Web.Components.Profile;
using MudBlazor;
using System;
using System.Threading.Tasks;

namespace Chronos.Web.Shared
{
    public partial class MainLayout : LayoutComponentBase
    {
        private ComponentModel _model { get; set; }
        private bool _drawerOpen = true;

        [Inject] private IStorage<string, UserProfile> _userProfileStorage { get; set; }
        [Inject] private IStorage<string, UserTheme> _userThemeStorage { get; set; }
        [Inject] private IStorage<string, UserWorklogFilter> _userWorklogFilterStorage { get; set; }
        [Inject] private IIdentityService _identityService { get; set; }
        [Inject] private IMediator _mediator { get; set; }
        [Inject] private IDialogService _dialogService { get; set; }
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }

        protected async Task ToggleThemeAsync(bool value)
        {
            _model.Theme.IsDarkMode = value;

            var user = await _identityService.GetCurrentUserAsync();
            string key = user != null ? user.Key : default;
            var userTheme = await _userThemeStorage.GetValueAsync(key);
            userTheme ??= UserTheme.Create();
            userTheme.IsDarkMode = _model.Theme.IsDarkMode;
            await _userThemeStorage.UpdateAsync(key, userTheme);
        }

        /// <summary>
        /// Opens the profile with the user's working day settings (issue #241).
        /// </summary>
        protected async Task OpenProfileAsync()
        {
            try
            {
                var user = await _identityService.GetCurrentUserAsync();
                if (user == null)
                {
                    return;
                }

                var settings = await _mediator.Send(new GetUserSettings.Query(user.Username));
                var parameters = new DialogParameters
                {
                    { nameof(ProfileDialog.Username), user.Username },
                    { nameof(ProfileDialog.Avatar), _model.Profile.Avatar },
                    { nameof(ProfileDialog.Settings), settings }
                };
                await _dialogService.ShowAsync<ProfileDialog>("Профиль", parameters);
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        void ToggleDrawer()
        {
            _drawerOpen = !_drawerOpen;
        }

        protected override async Task OnInitializedAsync()
        {
            _model = ComponentModel.Create();
            var user = await _identityService.GetCurrentUserAsync();
            if (user != null)
            {
                var profile = await _userProfileStorage.GetValueAsync(user.Key);
                _model.Profile.Initialize(profile);

                var theme = await _userThemeStorage.GetValueAsync(user.Key);
                _model.Theme.Initialize(theme);
            }
            await base.OnInitializedAsync();
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await RenderThemeAsync();
                await RenderProfileAsync();
                await EnsureUserRecordsAsync();
                _model.Initialize();
                StateHasChanged();
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task RenderThemeAsync()
        {
            if (_model.Theme.IsInitialized)
            {
                return;
            }
            var user = await _identityService.GetCurrentUserAsync();
            var theme = await _userThemeStorage.GetValueAsync(user?.Key);
            _model.Theme.Initialize(theme);
            await _userThemeStorage.UpdateAsync(user?.Key, theme);
        }

        private async Task RenderProfileAsync()
        {
            if (_model.Profile.IsInitialized)
            {
                return;
            }

            var user = await _identityService.GetCurrentUserAsync();
            if (user == null)
            {
                return;
            }
            else
            {
                await _userProfileStorage.ForceInitAsync(user.Key);
                var profile = await _userProfileStorage.GetValueAsync(user.Key);
                _model.Profile.Initialize(profile);
            }
        }

        /// <summary>
        /// Seeds the records a user needs before the first search: the Jira extension
        /// (issue #242) and the working day settings (issue #241). Both used to be answered
        /// in the worklog filter, so both are carried over from the filter that user still
        /// has in local storage. Runs after the first render because local storage needs JS
        /// interop; seeding never overwrites records that already exist.
        /// </summary>
        private async Task EnsureUserRecordsAsync()
        {
            try
            {
                var user = await _identityService.GetCurrentUserAsync();
                if (user == null)
                {
                    return;
                }

                var legacyFilter = await _userWorklogFilterStorage.GetValueAsync(user.Key);

                await _mediator.Send(new EnsureJiraExtension.Command(
                    user.Username,
                    HasLegacyFilter: legacyFilter != null,
                    LegacyCommentWorklogTime: legacyFilter?.CommentWorklogTime));

                await _mediator.Send(new EnsureUserSettings.Command(
                    user.Username,
                    LegacySettings: ToUserSettings(legacyFilter)));
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        /// <summary>
        /// Working day the user had answered in the legacy filter. A value the filter never
        /// stored falls back to the default, so a half-filled filter still migrates.
        /// </summary>
        private static UserSettingsDto ToUserSettings(UserWorklogFilter legacyFilter)
        {
            if (legacyFilter == null)
            {
                return null;
            }

            var defaults = UserSettingsDto.Default;
            return new UserSettingsDto(
                WorkingStartTime: legacyFilter.DailyWorkingStartTime ?? defaults.WorkingStartTime,
                WorkingEndTime: legacyFilter.DailyWorkingEndTime ?? defaults.WorkingEndTime,
                LunchTime: legacyFilter.LunchTime ?? defaults.LunchTime);
        }

        protected async Task Logout()
        {
            var user = await _identityService.GetCurrentUserAsync();
            if (user != null)
            {
                await _userProfileStorage.RemoveAsync(user.Key);
                await _userThemeStorage.RemoveAsync(user.Key);
            }
        }

        public class ComponentModel
        {
            public static ComponentModel Create()
            {
                return new ComponentModel();
            }

            public Theme Theme { get; set; } = new Theme();
            public Profile Profile { get; set; } = new Profile();
            public bool IsInitialized { get; private set; }
            public bool InProgress => !IsInitialized;

            public void Initialize()
            {
                IsInitialized = true;
            }
        }

        public class Theme
        {
            public bool IsDarkMode { get; set; }
            public bool IsInitialized { get; set; }

            public void Initialize(UserTheme theme)
            {
                if (theme != null)
                {
                    IsDarkMode = theme.IsDarkMode;
                    IsInitialized = true;
                }
            }
        }

        public class Profile
        {
            public string Username { get; set; }
            public string Avatar { get; set; } = string.Empty;
            public bool IsInitialized { get; set; }

            public void Initialize(UserProfile profile)
            {
                if (profile != null)
                {
                    Avatar = profile.Avatar;
                    Username = profile.Username;
                    IsInitialized = true;
                }
            }
        }
    }
}
