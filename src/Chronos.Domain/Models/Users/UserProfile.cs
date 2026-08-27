using Chronos.Domain.Models.Abstract;
using System;
using TimeZoneConverter;

namespace Chronos.Domain.Models.Users
{
    public class UserProfile : IEntity<string>
    {
        public string Username { get; set; }

        /// <summary>
        /// Read-only account details taken from Jira and shown in the profile (issue #241).
        /// </summary>
        public string DisplayName { get; set; }
        public string Email { get; set; }

        public string TimeZoneId { get; set; }
        public string Avatar { get; set; }
        public bool UseDarkMode { get; set; }
        public TimeZoneInfo TimeZoneInfo => TimeZoneId != null 
            ? TZConvert.GetTimeZoneInfo(TimeZoneId)
            : TimeZoneInfo.Local;

        public string Key => Username;

        public static UserProfile Create()
        {
            return new UserProfile();
        }

        public void UpdateUserInfo(UserProfile userProfile)
        {
            Username = userProfile.Username;
            DisplayName = userProfile.DisplayName;
            Email = userProfile.Email;
            TimeZoneId = userProfile.TimeZoneId;
            Avatar = userProfile.Avatar;
        }

        public void UpdateUseDarkMode(bool useDarkMode)
        {
            UseDarkMode = useDarkMode;
        }
    }
}
