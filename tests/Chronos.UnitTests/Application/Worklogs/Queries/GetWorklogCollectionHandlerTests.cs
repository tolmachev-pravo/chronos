using MediatR;
using Moq;
using NUnit.Framework;
using Chronos.Application.Authentication;
using Chronos.Application.Events.Queries;
using Chronos.Application.Users.Dto;
using Chronos.Application.Users.Queries;
using Chronos.Application.Worklogs.Queries;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Users;
using Chronos.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.UnitTests.Application.Worklogs.Queries
{
    /// <summary>
    /// The handler builds days from two streams — worklogs and events. Which sources
    /// exist, whether they are enabled and how they degrade is the concern of
    /// IEventDataSource and its providers, covered by their own tests. See issue #299.
    /// </summary>
    [TestFixture]
    public class GetWorklogCollectionHandlerTests
    {
        private Mock<IMediator> _mediatorMock;
        private Mock<IIdentityService> _identityServiceMock;
        private GetWorklogCollection.QueryHandler _sut;

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _identityServiceMock = new Mock<IIdentityService>();

            SetupEvents();
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetIssueWorklogs.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<IWorklog>)new List<IWorklog>());
            _identityServiceMock
                .Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(new User { Username = "user1" });
            SetupUserSettings(UserSettingsDto.Default);

            _sut = new GetWorklogCollection.QueryHandler(
                _mediatorMock.Object,
                _identityServiceMock.Object);
        }

        private static GetWorklogCollection.Query SingleDayQuery() => new()
        {
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 1)
        };

        private void SetupUserSettings(UserSettingsDto settings) =>
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetUserSettings.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);

        private void SetupEvents(params IEvent[] events) =>
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetUserEvents.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<IEvent>)events.ToList());

        private static UserEvent CalendarEvent(string summary, DateTime start, DateTime end, string? issueKey) =>
            new()
            {
                StartDate = start,
                CompleteDate = end,
                Summary = summary,
                Author = "user1",
                Source = EventSource.Calendar,
                Issue = issueKey is null
                    ? null
                    : new Issue { Key = issueKey, Identifier = issueKey, Summary = summary }
            };

        [Test]
        public async Task Handle_KeyedCalendarEvent_BecomesEstimatedCalendarWorklog()
        {
            SetupEvents(CalendarEvent(
                "PROJ-1 sync",
                new DateTime(2026, 6, 1, 11, 0, 0),
                new DateTime(2026, 6, 1, 13, 0, 0),
                "PROJ-1"));

            var result = await _sut.Handle(SingleDayQuery());

            var day = result.WorkingDays.Single();
            var calendarEstimated = day.EstimatedWorklogs
                .Where(w => w.Source == EventSource.Calendar)
                .ToList();
            Assert.That(calendarEstimated, Has.Count.EqualTo(1));
            Assert.That(calendarEstimated[0].Issue.Key, Is.EqualTo("PROJ-1"));
        }

        [Test]
        public async Task Handle_KeylessCalendarEvent_AddsToBlockedEventsAndBlockedTime()
        {
            SetupEvents(CalendarEvent(
                "Team lunch",
                new DateTime(2026, 6, 1, 12, 0, 0),
                new DateTime(2026, 6, 1, 13, 0, 0),
                null));

            var result = await _sut.Handle(SingleDayQuery());

            var day = result.WorkingDays.Single();
            Assert.That(day.BlockedEventsTime, Is.EqualTo(TimeSpan.FromHours(1)));
            Assert.That(day.BlockedEvents, Has.Count.EqualTo(1));
            Assert.That(day.BlockedEvents[0].Summary, Is.EqualTo("Team lunch"));
            Assert.That(day.BlockedEvents[0].StartDate, Is.EqualTo(new DateTime(2026, 6, 1, 12, 0, 0)));
            Assert.That(day.BlockedEvents[0].CompleteDate, Is.EqualTo(new DateTime(2026, 6, 1, 13, 0, 0)));
            Assert.That(day.EstimatedWorklogs.Any(w => w.Source == EventSource.Calendar), Is.False);
        }

        [Test]
        public async Task Handle_JiraEvent_BecomesEstimatedWorklogWithItsSource()
        {
            SetupEvents(new UserEvent
            {
                StartDate = new DateTime(2026, 6, 1, 10, 0, 0),
                CompleteDate = new DateTime(2026, 6, 1, 12, 0, 0),
                Author = "user1",
                Source = EventSource.Assignee,
                Issue = new Issue { Key = "PROJ-2", Identifier = "PROJ-2" }
            });

            var result = await _sut.Handle(SingleDayQuery());

            var day = result.WorkingDays.Single();
            var estimated = day.EstimatedWorklogs.Single();
            Assert.That(estimated.Source, Is.EqualTo(EventSource.Assignee));
            Assert.That(estimated.Issue.Key, Is.EqualTo("PROJ-2"));
        }

        [Test]
        public async Task Handle_NoEvents_NoBlockedTimeNoEstimatedWorklogs()
        {
            var result = await _sut.Handle(SingleDayQuery());

            var day = result.WorkingDays.Single();
            Assert.That(day.BlockedEventsTime, Is.EqualTo(TimeSpan.Zero));
            Assert.That(day.BlockedEvents, Is.Empty);
            Assert.That(day.EstimatedWorklogs, Is.Empty);
        }

        [Test]
        public async Task Handle_AsksForEventsAndWorklogsOnce()
        {
            await _sut.Handle(SingleDayQuery());

            _mediatorMock.Verify(
                x => x.Send(It.IsAny<GetUserEvents.Query>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _mediatorMock.Verify(
                x => x.Send(It.IsAny<GetIssueWorklogs.Query>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_TakesTheWorkingDayFromTheUserSettings()
        {
            SetupUserSettings(new UserSettingsDto(
                WorkingStartTime: TimeSpan.FromHours(9),
                WorkingEndTime: TimeSpan.FromHours(18),
                LunchTime: TimeSpan.FromMinutes(30)));

            var result = await _sut.Handle(SingleDayQuery());

            var day = result.WorkingDays.Single();
            Assert.Multiple(() =>
            {
                Assert.That(day.Settings.WorkingStartTime, Is.EqualTo(TimeSpan.FromHours(9)));
                Assert.That(day.Settings.WorkingEndTime, Is.EqualTo(TimeSpan.FromHours(18)));
                Assert.That(day.Settings.LunchTime, Is.EqualTo(TimeSpan.FromMinutes(30)));
                Assert.That(day.Settings.WorkingTime, Is.EqualTo(TimeSpan.FromHours(8.5)));
            });
        }
    }
}
