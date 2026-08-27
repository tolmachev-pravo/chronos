using Chronos.Domain.Models.Users;

namespace Chronos.Infrastructure.Jira.Dto
{
    public class UserDto
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string TimeZoneId { get; set; }
        public string Avatar { get; set; }

        public UserProfile ConvertToUserProfile()
        {
            return new UserProfile
            {
                Username = Username,
                DisplayName = DisplayName,
                Email = Email,
                TimeZoneId = TimeZoneId,
                Avatar = Avatar
            };
        }
    }
}
