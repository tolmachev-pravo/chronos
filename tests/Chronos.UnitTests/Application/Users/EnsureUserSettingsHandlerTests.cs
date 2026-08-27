using Moq;
using NUnit.Framework;
using Chronos.Application.Users;
using Chronos.Application.Users.Commands;
using Chronos.Application.Users.Dto;
using Chronos.Domain.Entities.Users;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.UnitTests.Application.Users
{
    [TestFixture]
    public class EnsureUserSettingsHandlerTests
    {
        private Mock<IUserSettingsRepository> _repoMock = null!;
        private UserSettings? _saved;

        [SetUp]
        public void SetUp()
        {
            _saved = null;
            _repoMock = new Mock<IUserSettingsRepository>();
            _repoMock.Setup(r => r.UpsertAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>()))
                     .Callback<UserSettings, CancellationToken>((settings, _) => _saved = settings)
                     .Returns(Task.CompletedTask);
        }

        [Test]
        public async Task Handle_WithoutStoredSettings_SeedsDefaults()
        {
            _repoMock.Setup(r => r.GetAsync("alice", CancellationToken.None))
                     .ReturnsAsync((UserSettings?)null);

            await CreateHandler().Handle(new EnsureUserSettings.Command("alice", null!), CancellationToken.None);

            Assert.That(_saved, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_saved!.Username, Is.EqualTo("alice"));
                Assert.That(_saved.WorkingStartTime, Is.EqualTo(UserSettingsDto.Default.WorkingStartTime));
                Assert.That(_saved.WorkingEndTime, Is.EqualTo(UserSettingsDto.Default.WorkingEndTime));
                Assert.That(_saved.LunchTime, Is.EqualTo(UserSettingsDto.Default.LunchTime));
            });
        }

        [Test]
        public async Task Handle_WithLegacyFilter_CarriesTheWorkingDayOver()
        {
            _repoMock.Setup(r => r.GetAsync("alice", CancellationToken.None))
                     .ReturnsAsync((UserSettings?)null);

            await CreateHandler().Handle(
                new EnsureUserSettings.Command("alice", new UserSettingsDto(
                    TimeSpan.FromHours(8), TimeSpan.FromHours(17), TimeSpan.FromMinutes(30))),
                CancellationToken.None);

            Assert.That(_saved, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_saved!.WorkingStartTime, Is.EqualTo(TimeSpan.FromHours(8)));
                Assert.That(_saved.WorkingEndTime, Is.EqualTo(TimeSpan.FromHours(17)));
                Assert.That(_saved.LunchTime, Is.EqualTo(TimeSpan.FromMinutes(30)));
            });
        }

        [Test]
        public async Task Handle_WithStoredSettings_KeepsThemUntouched()
        {
            _repoMock.Setup(r => r.GetAsync("alice", CancellationToken.None))
                     .ReturnsAsync(new UserSettings { Username = "alice" });

            await CreateHandler().Handle(
                new EnsureUserSettings.Command("alice", UserSettingsDto.Default),
                CancellationToken.None);

            _repoMock.Verify(r => r.UpsertAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithoutUsername_DoesNothing()
        {
            await CreateHandler().Handle(new EnsureUserSettings.Command(null!, null!), CancellationToken.None);

            _repoMock.Verify(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _repoMock.Verify(r => r.UpsertAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private EnsureUserSettings.Handler CreateHandler() => new(_repoMock.Object);
    }
}
