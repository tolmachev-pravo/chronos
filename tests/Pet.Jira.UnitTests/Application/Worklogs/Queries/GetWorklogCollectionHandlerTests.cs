using MediatR;
using Moq;
using NUnit.Framework;
using Pet.Jira.Application.Authentication;
using Pet.Jira.Application.Extensions.YandexCalendar.Dto;
using Pet.Jira.Application.Extensions.YandexCalendar.Queries;
using Pet.Jira.Application.Worklogs.Queries;
using Pet.Jira.Domain.Models.Users;
using Pet.Jira.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.UnitTests.Application.Worklogs.Queries
{
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

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetAssigneeJiraEvents.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<IWorklog>)new List<IWorklog>());
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetTesterJiraEvents.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<IWorklog>)new List<IWorklog>());
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetCommentJiraEvents.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<IWorklog>)new List<IWorklog>());
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetIssueWorklogs.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<IWorklog>)new List<IWorklog>());
            _identityServiceMock
                .Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(new User { Username = "user1" });
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetYandexCalendarEvents.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<YandexCalendarEventDto>)new List<YandexCalendarEventDto>());

            _sut = new GetWorklogCollection.QueryHandler(
                _mediatorMock.Object,
                _identityServiceMock.Object);
        }

        private static GetWorklogCollection.Query SingleDayQuery() => new()
        {
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 1),
            DailyWorkingStartTime = TimeSpan.FromHours(10),
            DailyWorkingEndTime = TimeSpan.FromHours(19),
            LunchTime = TimeSpan.FromHours(1),
            IssueStatusId = "3",
            CommentWorklogTime = TimeSpan.FromMinutes(10)
        };

        private static YandexCalendarEventDto Event(string summary, DateTime start, DateTime end, string? hint) =>
            new(Summary: summary, Start: start, End: end, JiraIssueKeyHint: hint);

        [Test]
        public async Task Handle_KeyedCalendarEvent_BecomesEstimatedCalendarWorklog()
        {
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetYandexCalendarEvents.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<YandexCalendarEventDto>)new List<YandexCalendarEventDto>
                {
                    Event("PROJ-1 sync", new DateTime(2026, 6, 1, 11, 0, 0), new DateTime(2026, 6, 1, 13, 0, 0), "PROJ-1")
                });

            var result = await _sut.Handle(SingleDayQuery());

            var day = result.WorkingDays.Single();
            var calendarEstimated = day.EstimatedWorklogs
                .Where(w => w.Source == WorklogSource.Calendar)
                .ToList();
            Assert.That(calendarEstimated, Has.Count.EqualTo(1));
            Assert.That(calendarEstimated[0].Issue.Key, Is.EqualTo("PROJ-1"));
        }

        [Test]
        public async Task Handle_KeylessCalendarEvent_AddsToBlockedCalendarEventsAndBlockedTime()
        {
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetYandexCalendarEvents.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<YandexCalendarEventDto>)new List<YandexCalendarEventDto>
                {
                    Event("Team lunch", new DateTime(2026, 6, 1, 12, 0, 0), new DateTime(2026, 6, 1, 13, 0, 0), null)
                });

            var result = await _sut.Handle(SingleDayQuery());

            var day = result.WorkingDays.Single();
            Assert.That(day.CalendarBlockedTime, Is.EqualTo(TimeSpan.FromHours(1)));
            Assert.That(day.BlockedCalendarEvents, Has.Count.EqualTo(1));
            Assert.That(day.BlockedCalendarEvents[0].Title, Is.EqualTo("Team lunch"));
            Assert.That(day.BlockedCalendarEvents[0].Start, Is.EqualTo(new DateTime(2026, 6, 1, 12, 0, 0)));
            Assert.That(day.BlockedCalendarEvents[0].End, Is.EqualTo(new DateTime(2026, 6, 1, 13, 0, 0)));
            Assert.That(day.EstimatedWorklogs.Any(w => w.Source == WorklogSource.Calendar), Is.False);
        }

        [Test]
        public async Task Handle_NoCalendarEvents_NoBlockedTimeNoCalendarWorklogs()
        {
            var result = await _sut.Handle(SingleDayQuery());

            var day = result.WorkingDays.Single();
            Assert.That(day.CalendarBlockedTime, Is.EqualTo(TimeSpan.Zero));
            Assert.That(day.BlockedCalendarEvents, Is.Empty);
            Assert.That(day.EstimatedWorklogs.Any(w => w.Source == WorklogSource.Calendar), Is.False);
        }

        [Test]
        public async Task Handle_CalendarFetchThrows_DegradesGracefully()
        {
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetYandexCalendarEvents.Query>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("calendar down"));

            var result = await _sut.Handle(SingleDayQuery());

            var day = result.WorkingDays.Single();
            Assert.That(day.CalendarBlockedTime, Is.EqualTo(TimeSpan.Zero));
            Assert.That(day.BlockedCalendarEvents, Is.Empty);
            Assert.That(day.EstimatedWorklogs.Any(w => w.Source == WorklogSource.Calendar), Is.False);
        }
    }
}
