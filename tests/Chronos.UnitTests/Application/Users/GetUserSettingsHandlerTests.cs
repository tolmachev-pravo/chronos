using Moq;
using NUnit.Framework;
using Chronos.Application.Users;
using Chronos.Application.Users.Dto;
using Chronos.Application.Users.Queries;
using Chronos.Domain.Entities.Users;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.UnitTests.Application.Users
{
    [TestFixture]
    public class GetUserSettingsHandlerTests
    {
        private Mock<IUserSettingsRepository> _repoMock = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IUserSettingsRepository>();
        }

        [Test]
        public async Task Handle_WithoutStoredSettings_ReturnsDefaults()
        {
            _repoMock.Setup(r => r.GetAsync("alice", CancellationToken.None))
                     .ReturnsAsync((UserSettings?)null);

            var result = await CreateHandler().Handle(new GetUserSettings.Query("alice"), CancellationToken.None);

            Assert.That(result, Is.EqualTo(UserSettingsDto.Default));
        }

        [Test]
        public async Task Handle_WithStoredSettings_ReturnsStoredWorkingDay()
        {
            _repoMock.Setup(r => r.GetAsync("alice", CancellationToken.None))
                     .ReturnsAsync(new UserSettings
                     {
                         Username = "alice",
                         WorkingStartTime = TimeSpan.FromHours(9),
                         WorkingEndTime = TimeSpan.FromHours(18),
                         LunchTime = TimeSpan.FromMinutes(45)
                     });

            var result = await CreateHandler().Handle(new GetUserSettings.Query("alice"), CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.WorkingStartTime, Is.EqualTo(TimeSpan.FromHours(9)));
                Assert.That(result.WorkingEndTime, Is.EqualTo(TimeSpan.FromHours(18)));
                Assert.That(result.LunchTime, Is.EqualTo(TimeSpan.FromMinutes(45)));
            });
        }

        [Test]
        public async Task Handle_WithoutUsername_ReturnsDefaultsWithoutTouchingTheRepository()
        {
            var result = await CreateHandler().Handle(new GetUserSettings.Query(null!), CancellationToken.None);

            Assert.That(result, Is.EqualTo(UserSettingsDto.Default));
            _repoMock.Verify(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private GetUserSettings.Handler CreateHandler() => new(_repoMock.Object);
    }
}
