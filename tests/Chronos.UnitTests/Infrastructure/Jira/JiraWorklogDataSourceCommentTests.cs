using Atlassian.Jira;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Chronos.Application.Authentication;
using Chronos.Application.Storage;
using Chronos.Application.Time;
using Chronos.Application.Worklogs.Queries;
using Chronos.Domain.Models.Users;
using Chronos.Infrastructure.Jira;
using Chronos.Infrastructure.Jira.Dto;
using Chronos.Infrastructure.Jira.Query;

namespace Chronos.UnitTests.Infrastructure.Jira
{
    /// <summary>
    /// The comment events are searched with the ScriptRunner "commented" issue function
    /// so the period filtering happens on the Jira side. See issue #259.
    /// </summary>
    [TestFixture]
    public class JiraWorklogDataSourceCommentTests
    {
        private const string CurrentUser = "d.tolmachev";

        private Mock<IJiraService> _jiraServiceMock;
        private Mock<IIdentityService> _identityServiceMock;
        private Mock<ITimeProvider> _timeProviderMock;
        private Mock<IStorage<string, UserProfile>> _userProfileStorageMock;
        private JiraConfiguration _configuration;

        [SetUp]
        public void SetUp()
        {
            _jiraServiceMock = new Mock<IJiraService>();
            _identityServiceMock = new Mock<IIdentityService>();
            _timeProviderMock = new Mock<ITimeProvider>();
            _userProfileStorageMock = new Mock<IStorage<string, UserProfile>>();
            _configuration = new JiraConfiguration();

            _identityServiceMock
                .Setup(mock => mock.GetCurrentUserAsync())
                .ReturnsAsync(new User { Username = CurrentUser });
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
        }

        private JiraWorklogDataSource CreateSut() => new(
            _jiraServiceMock.Object,
            new JiraQueryFactory(),
            _identityServiceMock.Object,
            _timeProviderMock.Object,
            _userProfileStorageMock.Object,
            Options.Create(_configuration),
            Mock.Of<ILogger<JiraWorklogDataSource>>());

        private static GetCommentJiraEvents.Query Query() => new()
        {
            StartDate = new DateTime(2026, 07, 01),
            EndDate = new DateTime(2026, 07, 15, 23, 59, 59),
            CommentWorklogTime = TimeSpan.FromMinutes(15)
        };

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
        public async Task GetCommentRawIssueWorklogsAsync_Should_UseTheCommentedIssueFunction_When_ScriptRunnerEnabled()
        {
            // Arrange
            var captured = CapturedJql();

            // Act
            await CreateSut().GetCommentRawIssueWorklogsAsync(Query());

            // Assert — the end bound is shifted a day forward because "before" is an
            // exclusive midnight, and the watcher condition is no longer needed.
            Assert.That(captured, Has.Count.EqualTo(1));
            Assert.That(captured.Single(), Is.EqualTo(
                "issueFunction in commented(\"by currentUser() after 2026/07/01 before 2026/07/16\") "
                + "AND assignee != currentUser() "
                + "ORDER BY updatedDate DESC "));
        }

        [Test]
        public async Task GetCommentRawIssueWorklogsAsync_Should_UseThePlainQuery_When_ScriptRunnerDisabled()
        {
            // Arrange
            _configuration.ScriptRunner.Enabled = false;
            var captured = CapturedJql();

            // Act
            await CreateSut().GetCommentRawIssueWorklogsAsync(Query());

            // Assert
            Assert.That(captured, Has.Count.EqualTo(1));
            Assert.That(captured.Single(), Is.EqualTo(
                "updatedDate >= '2026/07/01' "
                + "AND watcher = currentUser() "
                + "AND assignee != currentUser() "
                + "ORDER BY updatedDate DESC "));
        }

        [Test]
        public async Task GetCommentRawIssueWorklogsAsync_Should_FallBackToThePlainQuery_When_TheCommentedSearchFails()
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
            var result = await CreateSut().GetCommentRawIssueWorklogsAsync(Query());

            // Assert — both queries were attempted, in that order, and no exception escaped.
            Assert.That(result, Is.Empty);
            Assert.That(captured, Has.Count.EqualTo(2));
            Assert.That(captured[0], Does.Contain("issueFunction in commented"));
            Assert.That(captured[1], Does.Contain("watcher = currentUser()"));
        }

        [Test]
        public async Task GetCommentRawIssueWorklogsAsync_Should_ReturnTheCommentsAuthoredByTheCurrentUserInsideThePeriod()
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
            var result = (await CreateSut().GetCommentRawIssueWorklogsAsync(Query())).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Single().Issue.Key, Is.EqualTo("CASEM-1"));
            Assert.That(result.Single().Author, Is.EqualTo(CurrentUser));
        }
    }
}
