using MediatR;
using Microsoft.AspNetCore.Components;
using Pet.Jira.Application.Authentication;
using Pet.Jira.Application.Extensions.Jira.Commands;
using Pet.Jira.Application.Storage;
using Pet.Jira.Application.Users;
using Pet.Jira.Application.Worklogs.Dto;
using Pet.Jira.Domain.Models.Users;
using System;
using System.Threading.Tasks;

namespace Pet.Jira.Web.Shared
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
                await EnsureJiraExtensionAsync();
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
        /// Connects the Jira extension for a user that has none yet (issue #242). Runs after
        /// the first render because the legacy worklog filter lives in local storage and
        /// needs JS interop; users who already have that filter keep their current behaviour.
        /// </summary>
        private async Task EnsureJiraExtensionAsync()
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
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
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
