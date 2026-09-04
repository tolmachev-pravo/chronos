using MediatR;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using Moq;
using Chronos.Application.Authentication;
using Chronos.Application.Issues;
using Chronos.Application.Storage;
using Chronos.Application.Users.Dto;
using Chronos.Application.Users.Queries;
using Chronos.Application.Worklogs.Commands;
using Chronos.Application.Worklogs.Dto;
using Chronos.Application.Worklogs.Queries;
using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Users;
using Chronos.Domain.Models.Worklogs;
using Chronos.Web.Mcp.Tools;

namespace Chronos.UnitTests.Web.Mcp
{
    [TestFixture]
    public class WorklogToolsTests
    {
        private Mock<IMediator> _mediator = null!;
        private Mock<IIssueDataSource> _issueDataSource = null!;
        private Mock<IIdentityService> _identityService = null!;
        private Mock<IStorage<string, UserProfile>> _userProfileStorage = null!;
        private Mock<ILogger<WorklogTools>> _logger = null!;
        private WorklogTools _tools = null!;
        private DateTime _date;

        [SetUp]
        public void SetUp()
        {
            _date = new DateTime(2026, 9, 1);
            _mediator = new Mock<IMediator>();
            _issueDataSource = new Mock<IIssueDataSource>();
            _issueDataSource
                .Setup(source => source.GetIssueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string key, CancellationToken _) => new Issue
                {
                    Key = key,
                    Summary = $"Summary of {key}",
                    Link = $"https://jira/browse/{key}"
                });
            _identityService = new Mock<IIdentityService>();
            _identityService
                .Setup(service => service.GetCurrentUserAsync())
                .ReturnsAsync(new User { Username = "john" });

            _userProfileStorage = new Mock<IStorage<string, UserProfile>>();
            _logger = new Mock<ILogger<WorklogTools>>();

            _tools = new WorklogTools(
                _mediator.Object,
                _issueDataSource.Object,
                _identityService.Object,
                _userProfileStorage.Object,
                _logger.Object);
        }

        private void SetUpWorklogCollection(params WorkingDay[] days)
        {
            _mediator
                .Setup(mediator => mediator.Send(
                    It.IsAny<GetWorklogCollection.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetWorklogCollection.Model { WorkingDays = days });
        }

        private WorkingDay CreateDay(params WorkingDayWorklog[] worklogs)
        {
            return new WorkingDay(
                date: _date,
                settings: new WorkingDaySettings(
                    workingStartTime: TimeSpan.FromHours(10),
                    workingEndTime: TimeSpan.FromHours(19),
                    lunchTime: TimeSpan.FromHours(1)),
                worklogs: worklogs.ToList());
        }

        [Test]
        public async Task GetWorklogCollection_Should_ReportTheDay_AsLoggedAndSuggestedMinutes()
        {
            SetUpWorklogCollection(CreateDay(
                new WorkingDayWorklog
                {
                    Type = WorklogType.Actual,
                    StartDate = _date.AddHours(10),
                    CompleteDate = _date.AddHours(13),
                    RemainingTimeSpent = TimeSpan.FromHours(3),
                    Issue = new Issue { Key = "CH-1", Summary = "Logged" }
                },
                new WorkingDayWorklog
                {
                    Type = WorklogType.Estimated,
                    StartDate = _date.AddHours(14),
                    CompleteDate = _date.AddHours(19),
                    RemainingTimeSpent = TimeSpan.FromHours(5),
                    Issue = new Issue { Key = "CH-2", Summary = "Suggested" }
                }));

            var days = await _tools.GetWorklogCollection(_date, _date);

            Assert.That(days, Has.Count.EqualTo(1));
            var day = days[0];
            Assert.That(day.PlannedMinutes, Is.EqualTo(480));
            Assert.That(day.LoggedMinutes, Is.EqualTo(180));
            Assert.That(day.SuggestedMinutes, Is.EqualTo(300));
            Assert.That(day.Logged.Single().IssueKey, Is.EqualTo("CH-1"));
            Assert.That(day.Suggested.Single().IssueKey, Is.EqualTo("CH-2"));
            Assert.That(day.Suggested.Single().Minutes, Is.EqualTo(300));
        }

        [Test]
        public async Task GetWorklogCollection_Should_LoadTheProfile_BeforeAssemblingTheDay()
        {
            // The profile normally comes from the browser's local storage, which a client
            // request has none of. Unloaded, it leaves the day filtered by nobody.
            SetUpWorklogCollection(CreateDay());

            await _tools.GetWorklogCollection(_date, _date);

            _userProfileStorage.Verify(
                storage => storage.InitAsync("john", It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Test]
        public async Task AddWorklog_Should_LoadTheProfile_BeforeLoggingTheTime()
        {
            // The logged time is converted by the profile time zone: without the profile the
            // worklog would land in Jira at the wrong hour, or not land at all.
            _mediator
                .Setup(mediator => mediator.Send(It.IsAny<AddWorklog.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((object request, CancellationToken _) =>
                    new AddWorklog.Model { Worklog = ((AddWorklog.Command)request).Worklog });

            await _tools.AddWorklog("CH-101", _date.AddHours(10), minutes: 60);

            _userProfileStorage.Verify(
                storage => storage.InitAsync("john", It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Test]
        public async Task GetWorklogCollection_Should_LeaveOutASuggestion_AlreadyCoveredByAWorklog()
        {
            // A suggestion of zero is one the day found logged: offering it would invite the
            // client to log the same time twice.
            SetUpWorklogCollection(CreateDay(
                new WorkingDayWorklog
                {
                    Type = WorklogType.Estimated,
                    StartDate = _date.AddHours(14),
                    CompleteDate = _date.AddHours(15),
                    RemainingTimeSpent = TimeSpan.Zero,
                    Issue = new Issue { Key = "CH-2" }
                }));

            var days = await _tools.GetWorklogCollection(_date, _date);

            Assert.That(days.Single().Suggested, Is.Empty);
        }

        [Test]
        public void GetWorklogCollection_Should_Refuse_WhenThePeriodEndsBeforeItStarts()
        {
            Assert.ThrowsAsync<McpException>(
                () => _tools.GetWorklogCollection(_date, _date.AddDays(-1)));
        }

        [Test]
        public void GetWorklogCollection_Should_Refuse_APeriodLongerThanTwoMonths()
        {
            Assert.ThrowsAsync<McpException>(
                () => _tools.GetWorklogCollection(_date, _date.AddDays(62)));
        }

        [Test]
        public async Task GetUserSettings_Should_ReportTheWorkingDay_OfTheAuthenticatedUser()
        {
            _mediator
                .Setup(mediator => mediator.Send(It.IsAny<GetUserSettings.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserSettingsDto(
                    WorkingStartTime: TimeSpan.FromHours(9),
                    WorkingEndTime: TimeSpan.FromHours(18),
                    LunchTime: TimeSpan.FromMinutes(45)));

            var settings = await _tools.GetUserSettings();

            Assert.That(settings.Username, Is.EqualTo("john"));
            Assert.That(settings.WorkingStartTime, Is.EqualTo("09:00"));
            Assert.That(settings.WorkingEndTime, Is.EqualTo("18:00"));
            Assert.That(settings.LunchMinutes, Is.EqualTo(45));
            Assert.That(settings.WorkingMinutes, Is.EqualTo(495));
        }

        [Test]
        public void GetIssue_Should_Fail_WhenJiraKnowsNoSuchKey()
        {
            _issueDataSource
                .Setup(source => source.GetIssueAsync("CH-404", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Issue)null!);

            Assert.ThrowsAsync<McpException>(() => _tools.GetIssue("CH-404"));
        }

        [Test]
        public async Task AddWorklog_Should_LogTheTime_AgainstTheResolvedIssue()
        {
            AddWorklog.Command command = null!;
            _mediator
                .Setup(mediator => mediator.Send(It.IsAny<AddWorklog.Command>(), It.IsAny<CancellationToken>()))
                .Callback((object request, CancellationToken _) => command = (AddWorklog.Command)request)
                .ReturnsAsync((object request, CancellationToken _) =>
                    new AddWorklog.Model { Worklog = ((AddWorklog.Command)request).Worklog });

            var added = await _tools.AddWorklog("ch-101", _date.AddHours(10), minutes: 90, comment: "review");

            // The worklog carries the issue itself, not only its key: without it nothing
            // reaches Jira.
            Assert.That(command.Worklog.Issue, Is.Not.Null);
            Assert.That(command.Worklog.IssueKey, Is.EqualTo("ch-101"));
            Assert.That(command.Worklog.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(90)));
            Assert.That(command.Worklog.Comment, Is.EqualTo("review"));
            Assert.That(added.Minutes, Is.EqualTo(90));
            Assert.That(added.Summary, Is.EqualTo("Summary of ch-101"));
        }

        [Test]
        public async Task AddWorklog_Should_RecordWhatItWrote_SoALoggedTimeCanBeTracedToAClient()
        {
            // Jira remembers who logged the time, not what they logged it with. Without
            // this line nothing tells a worklog made by a client from one made in the site.
            _mediator
                .Setup(mediator => mediator.Send(It.IsAny<AddWorklog.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((object request, CancellationToken _) =>
                    new AddWorklog.Model { Worklog = ((AddWorklog.Command)request).Worklog });

            await _tools.AddWorklog("CH-101", _date.AddHours(10), minutes: 90, comment: "review");

            _logger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("CH-101") && state.ToString()!.Contains("90")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once());
        }

        [Test]
        public void AddWorklog_Should_Refuse_WhenTheIssueIsUnknown()
        {
            _issueDataSource
                .Setup(source => source.GetIssueAsync("CH-404", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Issue)null!);

            Assert.ThrowsAsync<McpException>(
                () => _tools.AddWorklog("CH-404", _date.AddHours(10), minutes: 60));
            _mediator.Verify(
                mediator => mediator.Send(It.IsAny<AddWorklog.Command>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [TestCase(0)]
        [TestCase(-30)]
        [TestCase(24 * 60 + 1)]
        public void AddWorklog_Should_Refuse_ATimeNobodyCouldHaveWorked(int minutes)
        {
            Assert.ThrowsAsync<McpException>(
                () => _tools.AddWorklog("CH-101", _date.AddHours(10), minutes));
            _mediator.Verify(
                mediator => mediator.Send(It.IsAny<AddWorklog.Command>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }
    }
}
