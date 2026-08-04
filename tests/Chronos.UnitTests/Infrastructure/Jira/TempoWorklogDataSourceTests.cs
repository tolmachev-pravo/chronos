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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.UnitTests.Infrastructure.Jira
{
    [TestFixture]
    public class TempoWorklogDataSourceTests
    {
        private const string CurrentUser = "d.tolmachev";

        private Mock<IJiraService> _jiraServiceMock;
        private Mock<IIdentityService> _identityServiceMock;
        private Mock<ITimeProvider> _timeProviderMock;
        private Mock<IStorage<string, UserProfile>> _userProfileStorageMock;
        private Mock<IJiraLinkGenerator> _linkGeneratorMock;
        private JiraWorklogDataSource _fallback;
        private TempoWorklogDataSource _sut;

        [SetUp]
        public void Setup()
        {
            _jiraServiceMock = new Mock<IJiraService>();
            _identityServiceMock = new Mock<IIdentityService>();
            _timeProviderMock = new Mock<ITimeProvider>();
            _userProfileStorageMock = new Mock<IStorage<string, UserProfile>>();
            _linkGeneratorMock = new Mock<IJiraLinkGenerator>();

            _identityServiceMock
                .Setup(mock => mock.GetCurrentUserAsync())
                .ReturnsAsync(new User { Username = CurrentUser });
            // Default: user and server in the same zone (+3) so basic mapping is a no-op.
            SetUserTimeZone("Europe/Moscow");
            SetServerUtcOffset(TimeSpan.FromHours(3));
            // Identity conversion for the fallback JiraWorklogDataSource path only.
            _timeProviderMock
                .Setup(mock => mock.ConvertToUserTimezone(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()))
                .Returns((DateTime dateTime, TimeZoneInfo _) => dateTime);
            _linkGeneratorMock
                .Setup(mock => mock.Generate(It.IsAny<string>()))
                .Returns((string key) => $"https://jira/browse/{key}");

            _fallback = new JiraWorklogDataSource(
                _jiraServiceMock.Object,
                new JiraQueryFactory(),
                _identityServiceMock.Object,
                _timeProviderMock.Object,
                _userProfileStorageMock.Object,
                Options.Create(new JiraConfiguration()),
                Mock.Of<ILogger<JiraWorklogDataSource>>());

            _sut = new TempoWorklogDataSource(
                _jiraServiceMock.Object,
                _identityServiceMock.Object,
                _userProfileStorageMock.Object,
                _linkGeneratorMock.Object,
                _fallback,
                Mock.Of<ILogger<TempoWorklogDataSource>>());
        }

        private void SetUserTimeZone(string timeZoneId)
            => _userProfileStorageMock
                .Setup(mock => mock.GetValueAsync(CurrentUser, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Username = CurrentUser, TimeZoneId = timeZoneId });

        private void SetServerUtcOffset(TimeSpan offset)
            => _jiraServiceMock
                .Setup(mock => mock.GetServerUtcOffsetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(offset);

        private static GetIssueWorklogs.Query Query() => new()
        {
            StartDate = new DateTime(2026, 7, 7, 0, 0, 0),
            EndDate = new DateTime(2026, 7, 14, 23, 59, 59)
        };

        private static TempoWorklogDto TempoWorklog(
            string author = CurrentUser,
            string? dateStarted = "2026-07-10T14:30:00",
            long timeSpentSeconds = 1800,
            string issueKey = "CASEM-1",
            string issueSummary = "Summary")
            => new()
            {
                Id = 1,
                TimeSpentInSeconds = timeSpentSeconds,
                DateStarted = dateStarted,
                Author = new TempoAuthor { Name = author },
                Issue = new TempoIssue { Id = 42, Key = issueKey, Summary = issueSummary }
            };

        private void SetupTempoWorklogs(params TempoWorklogDto[] worklogs)
            => _jiraServiceMock
                .Setup(mock => mock.GetTempoWorklogsAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(worklogs);

        [Test]
        public async Task GetIssueWorklogsAsync_Should_MapTempoWorklogToDomain()
        {
            // Arrange
            SetupTempoWorklogs(TempoWorklog());

            // Act
            var result = (await _sut.GetIssueWorklogsAsync(Query())).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            var worklog = result.Single();
            Assert.Multiple(() =>
            {
                Assert.That(worklog.StartDate, Is.EqualTo(new DateTime(2026, 7, 10, 14, 30, 0)));
                Assert.That(worklog.CompleteDate, Is.EqualTo(new DateTime(2026, 7, 10, 15, 0, 0)));
                Assert.That(worklog.TimeSpent, Is.EqualTo(TimeSpan.FromMinutes(30)));
                Assert.That(worklog.Author, Is.EqualTo(CurrentUser));
                Assert.That(worklog.Issue.Key, Is.EqualTo("CASEM-1"));
                Assert.That(worklog.Issue.Summary, Is.EqualTo("Summary"));
                Assert.That(worklog.Issue.Link, Is.EqualTo("https://jira/browse/CASEM-1"));
                Assert.That(worklog.Issue.Identifier, Is.EqualTo("42"));
            });
        }

        [Test]
        public async Task GetIssueWorklogsAsync_Should_ConvertServerTimeToUserTimezone()
        {
            // Arrange — Tempo returns a naive time in the Jira server zone (+3); user is +4.
            SetUserTimeZone("Asia/Dubai"); // +4, no DST
            SetServerUtcOffset(TimeSpan.FromHours(3));
            SetupTempoWorklogs(TempoWorklog(dateStarted: "2026-07-10T10:00:00", timeSpentSeconds: 3600));

            // Act
            var result = (await _sut.GetIssueWorklogsAsync(Query())).ToList();

            // Assert — 10:00 (+3) => 11:00 (+4).
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(result.Single().StartDate, Is.EqualTo(new DateTime(2026, 7, 10, 11, 0, 0)));
                Assert.That(result.Single().CompleteDate, Is.EqualTo(new DateTime(2026, 7, 10, 12, 0, 0)));
            });
        }

        [Test]
        public async Task GetIssueWorklogsAsync_Should_FilterOutOtherAuthors()
        {
            // Arrange
            SetupTempoWorklogs(
                TempoWorklog(author: CurrentUser, issueKey: "MINE-1"),
                TempoWorklog(author: "other.user", issueKey: "THEIRS-1"));

            // Act
            var result = (await _sut.GetIssueWorklogsAsync(Query())).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Single().Issue.Key, Is.EqualTo("MINE-1"));
        }

        [Test]
        public async Task GetIssueWorklogsAsync_Should_MatchAuthorCaseInsensitively()
        {
            // Arrange
            SetupTempoWorklogs(TempoWorklog(author: "D.Tolmachev"));

            // Act
            var result = (await _sut.GetIssueWorklogsAsync(Query())).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetIssueWorklogsAsync_Should_SkipWorklogsWithUnparsableDate()
        {
            // Arrange
            SetupTempoWorklogs(TempoWorklog(dateStarted: null));

            // Act
            var result = (await _sut.GetIssueWorklogsAsync(Query())).ToList();

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetIssueWorklogsAsync_Should_ExcludeWorklogsOutsideRange()
        {
            // Arrange — server returns a worklog past the requested end date.
            SetupTempoWorklogs(TempoWorklog(dateStarted: "2026-07-20T10:00:00"));

            // Act
            var result = (await _sut.GetIssueWorklogsAsync(Query())).ToList();

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetIssueWorklogsAsync_Should_FallBackToJira_When_TempoFails()
        {
            // Arrange
            _jiraServiceMock
                .Setup(mock => mock.GetTempoWorklogsAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Tempo down"));
            _jiraServiceMock
                .Setup(mock => mock.GetIssueWorklogsAsync(
                    It.IsAny<Atlassian.Jira.IssueSearchOptions>(),
                    It.IsAny<Func<Atlassian.Jira.Worklog, bool>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IssueWorklogDto>
                {
                    new()
                    {
                        StartDate = new DateTime(2026, 7, 9, 10, 0, 0),
                        TimeSpentInSeconds = 3600,
                        Issue = new IssueDto { Key = "FALLBACK-1", Summary = "s", Link = "l", Identifier = "7" }
                    }
                });

            // Act
            var result = (await _sut.GetIssueWorklogsAsync(Query())).ToList();

            // Assert — result comes from the Jira fallback path, not Tempo.
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Single().Issue.Key, Is.EqualTo("FALLBACK-1"));
            _jiraServiceMock.Verify(mock => mock.GetIssueWorklogsAsync(
                It.IsAny<Atlassian.Jira.IssueSearchOptions>(),
                It.IsAny<Func<Atlassian.Jira.Worklog, bool>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
