using NUnit.Framework;
using Chronos.Infrastructure.Jira.Dto;

namespace Chronos.UnitTests.Infrastructure.Jira
{
    [TestFixture]
    public class UserDtoTests
    {
        [Test]
        public void ConvertToUserProfile_CarriesTheJiraAccountDetails()
        {
            var dto = new UserDto
            {
                Username = "alice",
                DisplayName = "Alice Smith",
                Email = "alice@example.com",
                TimeZoneId = "Europe/Moscow",
                Avatar = "data:image/jpg;base64, AAA"
            };

            var profile = dto.ConvertToUserProfile();

            Assert.Multiple(() =>
            {
                Assert.That(profile.Username, Is.EqualTo("alice"));
                Assert.That(profile.DisplayName, Is.EqualTo("Alice Smith"));
                Assert.That(profile.Email, Is.EqualTo("alice@example.com"));
                Assert.That(profile.TimeZoneId, Is.EqualTo("Europe/Moscow"));
                Assert.That(profile.Avatar, Is.EqualTo("data:image/jpg;base64, AAA"));
            });
        }
    }
}
