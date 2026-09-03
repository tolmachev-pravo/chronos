using Atlassian.Jira;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Chronos.Application.Events;
using Chronos.Application.Extensions.Jira;
using Chronos.Application.Extensions.Jira.Dto;
using Chronos.Application.Storage;
using Chronos.Application.Time;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Users;
using Chronos.Infrastructure.Events;
using Chronos.Infrastructure.Jira;
using Chronos.Infrastructure.Jira.Dto;
using Chronos.Infrastructure.Jira.Query;

namespace Chronos.UnitTests.Infrastructure.Events
{
    /// <summary>
    /// The comment events are searched with the ScriptRunner "commented" issue function
    /// so the period filtering happens on the Jira side. See issue #259. Moved from
    /// JiraWorklogDataSourceCommentTests when comments became an event source (#299).
    /// </summary>
    [TestFixture]
    public class JiraCommentEventProviderTests
    {
        private const string CurrentUser = "d.tolmachev";

        private Mock<IJiraService> _jiraServiceMock;
        private Mock<IStorage<string, UserProfile>> _userProfileStorageMock;
        private Mock<ITimeProvider> _timeProviderMock;
        private Mock<IJiraExtensionProvider> _extensionProviderMock;
        private JiraConfiguration _configuration;

        [SetUp]
        public void SetUp()
        {
            _jiraServiceMock = new Mock<IJiraService>();
            _userProfileStorageMock = new Mock<IStorage<string, UserProfile>>();
            _timeProviderMock = new Mock<ITimeProvider>();
            _extensionProviderMock = new Mock<IJiraExtensionProvider>();
            _configuration = new JiraConfiguration();

            _userProfileStorageMock
                .Setup(mock => mock.GetValueAsync(CurrentUser, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Username = CurrentUser, TimeZoneId = "Europe/Moscow" });
            _timeProviderMock
                .Setup(mock => mock.ConvertToUserTimezone(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()))
                .Returns((DateTime dateTime, TimeZoneInfo _) => dateTime);
            _jiraServiceMock
                .Setup(mock => mock.GetIssuesAsync(
                    It.IsAny<IssueSearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<IssueDto>());
            SetupExtension(new JiraExtensionSettingsDto(
                AssigneeEventsEnabled: true,
                CommentEventsEnabled: true,
                CommentWorklogTime: TimeSpan.FromMinutes(15),
                TesterEventsEnabled: false));
        }

        private void SetupExtension(JiraExtensionSettingsDto settings, bool isEnabled = true) =>
            _extensionProviderMock
                .Setup(mock => mock.GetAsync(CurrentUser, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JiraExtensionDto(isEnabled, settings));

        private JiraCommentEventProvider CreateSut() => new(
            _jiraServiceMock.Object,
            new JiraQueryFactory(),
            _userProfileStorageMock.Object,
            _timeProviderMock.Object,
            _extensionProviderMock.Object,
            Options.Create(_configuration),
            Mock.Of<ILogger<JiraCommentEventProvider>>());

        private static EventQuery Query() => new(
            CurrentUser,
            new DateTime(2026, 07, 01),
            new DateTime(2026, 07, 15, 23, 59, 59));

        private async Task<IEnumerable<IEvent>> GetEventsAsync(JiraCommentEventProvider sut)
        {
            Assert.That(await sut.PrepareAsync(Query()), Is.True);
            return await sut.GetEventsAsync();
        }

        private List<string> CapturedJql()
        {
            var captured = new List<string>();
            _jiraServiceMock
                .Setup(mock => mock.GetIssuesAsync(
                    It.IsAny<IssueSearchOptions>(), It.IsAny<CancellationToken>()))
                .Callback((IssueSearchOptions options, CancellationToken _) => captured.Add(options.Jql))
                .ReturnsAsync(Array.Empty<IssueDto>());
            return captured;
        }

        [Test]
        public async Task GetEventsAsync_Should_UseTheCommentedIssueFunction_When_ScriptRunnerEnabled()
        {
            // Arrange
            var captured = CapturedJql();

            // Act
            await GetEventsAsync(CreateSut());

            // Assert — the end bound is shifted a day forward because "before" is an
            // exclusive midnight, and the watcher condition is no longer needed.
            Assert.That(captured, Has.Count.EqualTo(1));
            Assert.That(captured.Single(), Is.EqualTo(
                "issueFunction in commented(\"by currentUser() after 2026/07/01 before 2026/07/16\") "
                + "AND assignee != currentUser() "
                + "ORDER BY updatedDate DESC "));
        }

        [Test]
        public async Task GetEventsAsync_Should_UseThePlainQuery_When_ScriptRunnerDisabled()
        {
            // Arrange
            _configuration.ScriptRunner.Enabled = false;
            var captured = CapturedJql();

            // Act
            await GetEventsAsync(CreateSut());

            // Assert
            Assert.That(captured, Has.Count.EqualTo(1));
            Assert.That(captured.Single(), Is.EqualTo(
                "updatedDate >= '2026/07/01' "
                + "AND watcher = currentUser() "
                + "AND assignee != currentUser() "
                + "ORDER BY updatedDate DESC "));
        }

        [Test]
        public async Task GetEventsAsync_Should_FallBackToThePlainQuery_When_TheCommentedSearchFails()
        {
            // Arrange — the addon is missing, so Jira rejects the issue function.
            var captured = new List<string>();
            _jiraServiceMock
                .Setup(mock => mock.GetIssuesAsync(
                    It.IsAny<IssueSearchOptions>(), It.IsAny<CancellationToken>()))
                .Callback((IssueSearchOptions options, CancellationToken _) => captured.Add(options.Jql))
                .Returns((IssueSearchOptions options, CancellationToken _) =>
                    options.Jql.Contains("issueFunction")
                        ? throw new InvalidOperationException("Unable to find JQL function 'commented'")
                        : Task.FromResult<IEnumerable<IssueDto>>(Array.Empty<IssueDto>()));

            // Act
            var result = await GetEventsAsync(CreateSut());

            // Assert — both queries were attempted, in that order, and no exception escaped.
            Assert.That(result, Is.Empty);
            Assert.That(captured, Has.Count.EqualTo(2));
            Assert.That(captured[0], Does.Contain("issueFunction in commented"));
            Assert.That(captured[1], Does.Contain("watcher = currentUser()"));
        }

        [Test]
        public async Task GetEventsAsync_Should_ReturnTheCommentsAuthoredByTheCurrentUserInsideThePeriod()
        {
            // Arrange
            var issue = new IssueDto { Key = "CASEM-1", Summary = "s", Link = "l", Identifier = "1" };
            _jiraServiceMock
                .Setup(mock => mock.GetIssuesAsync(
                    It.IsAny<IssueSearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { issue });
            _jiraServiceMock
                .Setup(mock => mock.GetIssueCommentsAsync(
                    It.IsAny<IEnumerable<IssueDto>>(),
                    It.IsAny<Func<Comment, bool>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new IssueCommentDto
                    {
                        CreatedDate = new DateTime(2026, 07, 09, 10, 00, 00),
                        Author = CurrentUser,
                        Issue = issue
                    }
                });

            // Act
            var result = (await GetEventsAsync(CreateSut())).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Single().Issue.Key, Is.EqualTo("CASEM-1"));
            Assert.That(result.Single().Author, Is.EqualTo(CurrentUser));
            Assert.That(result.Single().Source, Is.EqualTo(EventSource.Comment));
            // The comment worklog time from the extension settings frames the event.
            Assert.That(result.Single().Duration, Is.EqualTo(TimeSpan.FromMinutes(15)));
        }

        [Test]
        public async Task PrepareAsync_Should_ReturnFalse_When_CommentEventsAreDisabled()
        {
            // Arrange
            SetupExtension(JiraExtensionSettingsDto.Default);

            // Act
            var prepared = await CreateSut().PrepareAsync(Query());

            // Assert — a disabled source is not queried at all. See issue #242.
            Assert.That(prepared, Is.False);
            _jiraServiceMock.Verify(
                mock => mock.GetIssuesAsync(It.IsAny<IssueSearchOptions>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
