using NUnit.Framework;
using Chronos.Application.Users.Commands;
using Chronos.Application.Users.Dto;
using System;

namespace Chronos.UnitTests.Application.Users
{
    [TestFixture]
    public class UpsertUserSettingsValidatorTests
    {
        private readonly UpsertUserSettingsValidator _sut = new();

        [Test]
        public void Validate_DefaultWorkingDay_IsValid()
        {
            var result = _sut.Validate(new UpsertUserSettings.Command("alice", UserSettingsDto.Default));

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_EndBeforeStart_IsInvalid()
        {
            var result = _sut.Validate(new UpsertUserSettings.Command("alice", new UserSettingsDto(
                TimeSpan.FromHours(19), TimeSpan.FromHours(10), TimeSpan.Zero)));

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_LunchLongerThanTheDay_IsInvalid()
        {
            var result = _sut.Validate(new UpsertUserSettings.Command("alice", new UserSettingsDto(
                TimeSpan.FromHours(10), TimeSpan.FromHours(12), TimeSpan.FromHours(3))));

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_WithoutUsername_IsInvalid()
        {
            var result = _sut.Validate(new UpsertUserSettings.Command(string.Empty, UserSettingsDto.Default));

            Assert.That(result.IsValid, Is.False);
        }
    }
}
