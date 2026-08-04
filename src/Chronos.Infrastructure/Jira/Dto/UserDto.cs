using Chronos.Domain.Models.Users;

namespace Chronos.Infrastructure.Jira.Dto
{
    public class UserDto
    {
        public string Username { get; set; }
        public string TimeZoneId { get; set; }
        public string Avatar { get; set; }

        public UserProfile ConvertToUserProfile()
        {
            return new UserProfile
            {
                Username = Username,
                TimeZoneId = TimeZoneId,
                Avatar = Avatar
            };
        }
    }
}
