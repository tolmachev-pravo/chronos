using Moq;
using NUnit.Framework;
using Chronos.Application.Extensions;
using Chronos.Application.Extensions.YandexCalendar;
using Chronos.Application.Extensions.YandexCalendar.Dto;
using Chronos.Application.Extensions.YandexCalendar.Queries;
using Chronos.Application.Storage;
using Chronos.Domain.Entities.Extensions;
using Chronos.Domain.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.UnitTests.Application.Extensions.YandexCalendar
{
    [TestFixture]
    public class GetYandexCalendarEventsHandlerTests
    {
        private Mock<IUserExtensionRepository> _repoMock = null!;
        private Mock<IYandexCalendarSettingsProvider> _settingsMock = null!;
        private Mock<IYandexCalendarService> _calMock = null!;
        private Mock<IStorage<string, UserProfile>> _profileStorageMock = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IUserExtensionRepository>();
            _settingsMock = new Mock<IYandexCalendarSettingsProvider>();
            _calMock = new Mock<IYandexCalendarService>();
            _profileStorageMock = new Mock<IStorage<string, UserProfile>>();

            _profileStorageMock
                .Setup(s => s.GetValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);
        }

        private GetYandexCalendarEvents.Handler CreateHandler() =>
            new(_repoMock.Object, _settingsMock.Object, _calMock.Object, _profileStorageMock.Object);

        [Test]
        public async Task Handle_ReturnsEvents_WhenExtensionEnabled()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.YandexCalendar, CancellationToken.None))
                     .ReturnsAsync(new UserExtension { IsEnabled = true });

            var dto = new YandexCalendarSettingsDto("user@yandex.ru", "pw", new List<string>(), new List<YandexCalendarIssueMapping>());
            _settingsMock.Setup(s => s.GetSettingsAsync("alice", CancellationToken.None))
                         .ReturnsAsync(dto);

            var date = new DateOnly(2026, 6, 4);
            var utcStart = new DateTime(2026, 6, 4, 10, 0, 0, DateTimeKind.Utc);
            var utcEnd   = new DateTime(2026, 6, 4, 11, 0, 0, DateTimeKind.Utc);
            var events = new List<YandexCalendarEventDto>
            {
                new("Standup PROJ-42", utcStart, utcEnd, "PROJ-42")
            };
            _calMock.Setup(c => c.GetEventsAsync(
                        It.Is<YandexCalendarCredentials>(cr => cr.Login == "user@yandex.ru"),
                        date,
                        It.IsAny<TimeZoneInfo>(),
                        CancellationToken.None))
                    .ReturnsAsync(events);

            var result = await CreateHandler().Handle(
                new GetYandexCalendarEvents.Query("alice", date), CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].JiraIssueKeyHint, Is.EqualTo("PROJ-42"));
        }

        [Test]
        public async Task Handle_FiltersExcludedPhrases()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.YandexCalendar, CancellationToken.None))
                     .ReturnsAsync(new UserExtension { IsEnabled = true });

            var dto = new YandexCalendarSettingsDto("user@yandex.ru", "pw", new List<string> { "Обед", "Lunch" }, new List<YandexCalendarIssueMapping>());
            _settingsMock.Setup(s => s.GetSettingsAsync("alice", CancellationToken.None))
                         .ReturnsAsync(dto);

            var date = new DateOnly(2026, 6, 4);
            var utcNow = new DateTime(2026, 6, 4, 10, 0, 0, DateTimeKind.Utc);
            var events = new List<YandexCalendarEventDto>
            {
                new("Standup", utcNow, utcNow.AddHours(1), null),
                new("Обед с командой", utcNow.AddHours(2), utcNow.AddHours(3), null),
                new("Team Lunch", utcNow.AddHours(3), utcNow.AddHours(4), null),
            };
            _calMock.Setup(c => c.GetEventsAsync(
                        It.IsAny<YandexCalendarCredentials>(),
                        date,
                        It.IsAny<TimeZoneInfo>(),
                        CancellationToken.None))
                    .ReturnsAsync(events);

            var result = await CreateHandler().Handle(
                new GetYandexCalendarEvents.Query("alice", date), CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Summary, Is.EqualTo("Standup"));
        }

        [Test]
        public async Task Handle_AppliesIssueMapping_WhenEventTitleMatchesExactly()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.YandexCalendar, CancellationToken.None))
                     .ReturnsAsync(new UserExtension { IsEnabled = true });

            var mappings = new List<YandexCalendarIssueMapping>
            {
                new("Core Daily Sync", "CASEM-73656")
            };
            var dto = new YandexCalendarSettingsDto("user@yandex.ru", "pw", new List<string>(), mappings);
            _settingsMock.Setup(s => s.GetSettingsAsync("alice", CancellationToken.None))
                         .ReturnsAsync(dto);

            var date = new DateOnly(2026, 6, 4);
            var utcNow = new DateTime(2026, 6, 4, 10, 0, 0, DateTimeKind.Utc);
            var events = new List<YandexCalendarEventDto>
            {
                new("Core Daily Sync", utcNow, utcNow.AddHours(1), null),
                new("Other Meeting",   utcNow.AddHours(2), utcNow.AddHours(3), null),
            };
            _calMock.Setup(c => c.GetEventsAsync(
                        It.IsAny<YandexCalendarCredentials>(),
                        date,
                        It.IsAny<TimeZoneInfo>(),
                        CancellationToken.None))
                    .ReturnsAsync(events);

            var result = await CreateHandler().Handle(
                new GetYandexCalendarEvents.Query("alice", date), CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].JiraIssueKeyHint, Is.EqualTo("CASEM-73656"), "Mapped event should get the issue key");
            Assert.That(result[1].JiraIssueKeyHint, Is.Null, "Unmapped event should have no hint");
        }

        [Test]
        public async Task Handle_AppliesIssueMapping_WhenEventTitleMatchesMask()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.YandexCalendar, CancellationToken.None))
                     .ReturnsAsync(new UserExtension { IsEnabled = true });

            var mappings = new List<YandexCalendarIssueMapping>
            {
                new("Core Daily*", "CASEM-73656"),
                new("Core Daily Sync", "CASEM-11111"),
            };
            var dto = new YandexCalendarSettingsDto("user@yandex.ru", "pw", new List<string>(), mappings);
            _settingsMock.Setup(s => s.GetSettingsAsync("alice", CancellationToken.None))
                         .ReturnsAsync(dto);

            var date = new DateOnly(2026, 6, 4);
            var utcNow = new DateTime(2026, 6, 4, 10, 0, 0, DateTimeKind.Utc);
            var events = new List<YandexCalendarEventDto>
            {
                new("Core Daily Sync (перенос)", utcNow, utcNow.AddHours(1), null),
                new("Core Daily Sync",           utcNow.AddHours(2), utcNow.AddHours(3), null),
                new("Retro",                     utcNow.AddHours(4), utcNow.AddHours(5), null),
            };
            _calMock.Setup(c => c.GetEventsAsync(
                        It.IsAny<YandexCalendarCredentials>(),
                        date,
                        It.IsAny<TimeZoneInfo>(),
                        CancellationToken.None))
                    .ReturnsAsync(events);

            var result = await CreateHandler().Handle(
                new GetYandexCalendarEvents.Query("alice", date), CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result[0].JiraIssueKeyHint, Is.EqualTo("CASEM-73656"), "Mask should cover the renamed event");
                Assert.That(result[1].JiraIssueKeyHint, Is.EqualTo("CASEM-11111"), "Exact phrase wins over the mask");
                Assert.That(result[2].JiraIssueKeyHint, Is.Null, "Unmapped event should have no hint");
            });
        }

        [Test]
        public async Task Handle_ReturnsEmpty_WhenExtensionDisabled()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.YandexCalendar, CancellationToken.None))
                     .ReturnsAsync(new UserExtension { IsEnabled = false });

            var result = await CreateHandler().Handle(
                new GetYandexCalendarEvents.Query("alice", new DateOnly(2026, 6, 4)),
                CancellationToken.None);

            Assert.That(result, Is.Empty);
            _settingsMock.Verify(s => s.GetSettingsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _calMock.Verify(c => c.GetEventsAsync(
                It.IsAny<YandexCalendarCredentials>(),
                It.IsAny<DateOnly>(),
                It.IsAny<TimeZoneInfo>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_ReturnsEmpty_WhenExtensionNotConfigured()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.YandexCalendar, CancellationToken.None))
                     .ReturnsAsync((UserExtension?)null);

            var result = await CreateHandler().Handle(
                new GetYandexCalendarEvents.Query("alice", new DateOnly(2026, 6, 4)),
                CancellationToken.None);

            Assert.That(result, Is.Empty);
            _calMock.Verify(c => c.GetEventsAsync(
                It.IsAny<YandexCalendarCredentials>(),
                It.IsAny<DateOnly>(),
                It.IsAny<TimeZoneInfo>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
