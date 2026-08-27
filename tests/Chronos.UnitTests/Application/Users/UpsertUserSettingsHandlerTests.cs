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
    public class UpsertUserSettingsHandlerTests
    {
        private Mock<IUserSettingsRepository> _repoMock = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IUserSettingsRepository>();
        }

        [Test]
        public async Task Handle_WithoutStoredSettings_CreatesRow()
        {
            _repoMock.Setup(r => r.GetAsync("alice", CancellationToken.None))
                     .ReturnsAsync((UserSettings?)null);
            var saved = CaptureUpsert();

            await CreateHandler().Handle(
                new UpsertUserSettings.Command("alice", new UserSettingsDto(
                    TimeSpan.FromHours(9), TimeSpan.FromHours(18), TimeSpan.FromMinutes(30))),
                CancellationToken.None);

            Assert.That(saved.Value, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(saved.Value!.Username, Is.EqualTo("alice"));
                Assert.That(saved.Value.WorkingStartTime, Is.EqualTo(TimeSpan.FromHours(9)));
                Assert.That(saved.Value.WorkingEndTime, Is.EqualTo(TimeSpan.FromHours(18)));
                Assert.That(saved.Value.LunchTime, Is.EqualTo(TimeSpan.FromMinutes(30)));
                Assert.That(saved.Value.CreatedAt, Is.Not.EqualTo(default(DateTime)));
            });
        }

        [Test]
        public async Task Handle_WithStoredSettings_UpdatesRowInPlace()
        {
            var existing = new UserSettings
            {
                Id = Guid.NewGuid(),
                Username = "alice",
                WorkingStartTime = TimeSpan.FromHours(10),
                WorkingEndTime = TimeSpan.FromHours(19),
                LunchTime = TimeSpan.FromHours(1),
                CreatedAt = new DateTime(2026, 1, 1)
            };
            _repoMock.Setup(r => r.GetAsync("alice", CancellationToken.None)).ReturnsAsync(existing);
            var saved = CaptureUpsert();

            await CreateHandler().Handle(
                new UpsertUserSettings.Command("alice", new UserSettingsDto(
                    TimeSpan.FromHours(8), TimeSpan.FromHours(17), TimeSpan.Zero)),
                CancellationToken.None);

            Assert.That(saved.Value, Is.SameAs(existing));
            Assert.Multiple(() =>
            {
                Assert.That(saved.Value!.CreatedAt, Is.EqualTo(new DateTime(2026, 1, 1)));
                Assert.That(saved.Value.WorkingStartTime, Is.EqualTo(TimeSpan.FromHours(8)));
                Assert.That(saved.Value.WorkingEndTime, Is.EqualTo(TimeSpan.FromHours(17)));
                Assert.That(saved.Value.LunchTime, Is.EqualTo(TimeSpan.Zero));
                Assert.That(saved.Value.UpdatedAt, Is.Not.Null);
            });
        }

        private sealed class Captured
        {
            public UserSettings? Value { get; set; }
        }

        private Captured CaptureUpsert()
        {
            var captured = new Captured();
            _repoMock.Setup(r => r.UpsertAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>()))
                     .Callback<UserSettings, CancellationToken>((settings, _) => captured.Value = settings)
                     .Returns(Task.CompletedTask);
            return captured;
        }

        private UpsertUserSettings.Handler CreateHandler() => new(_repoMock.Object);
    }
}
