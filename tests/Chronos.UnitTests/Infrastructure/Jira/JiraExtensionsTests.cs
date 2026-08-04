using Moq;
using Chronos.Application.Time;
using Chronos.Domain.Models.Worklogs;
using Chronos.Infrastructure.Jira;
using Chronos.Infrastructure.Jira.Dto;
using System.Collections;

namespace Chronos.UnitTests.Infrastructure.Jira
{
    [TestFixture]
    public class JiraExtensionsTests
    {
        private ITimeProvider _timeProvider;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var timeProviderMock = new Mock<ITimeProvider>();
            timeProviderMock
                .Setup(mock => mock.ConvertToUserTimezone(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()))
                .Returns((DateTime dateTime, TimeZoneInfo userTimeZone) => dateTime);
            _timeProvider = timeProviderMock.Object;
        }

        class CommentConvertToCases : IEnumerable
        {
            public IEnumerator GetEnumerator()
            {
                {
                    yield return new object[] {
                        new IssueCommentDto
                        {
                            CreatedDate = new DateTime(2022, 01, 03, 13, 22, 10),
                            Author = "user1",
                            Issue = new Mock<IssueDto>().Object
                        }
                    };
                }
            }
        }

        [TestCaseSource(typeof(CommentConvertToCases))]
        public void CommentConvertTo_Should_BeCorrect(IssueCommentDto comment)
        {
            // Arrange
            List<IssueCommentDto> comments = new() { comment };

            // Act
            var worklogs = comments.ConvertTo<RawIssueWorklog>(_timeProvider, It.IsAny<TimeZoneInfo>(), WorklogSource.Comment, TimeSpan.FromMinutes(10));

            // Assert
            var worklog = worklogs.Single();
            Assert.Multiple(() =>
            {
                Assert.That(worklog.Author, Is.EqualTo(comment.Author));
                Assert.That(worklog.TimeSpent, Is.EqualTo(TimeSpan.FromMinutes(10)));
                Assert.That(worklog.CompleteDate, Is.EqualTo(comment.CreatedDate));
                Assert.That(worklog.StartDate, Is.EqualTo(comment.CreatedDate.AddMinutes(-10)));
                Assert.That(worklog.Source, Is.EqualTo(WorklogSource.Comment));
            });
        }

        private static class IssueStatus
        {
            public static string InProgress = "In Progress";
            public static string Open = "Open";
            public static string InReview = "In Review";
        }

        class ChangeLogItemConvertToCases : IEnumerable
        {
            private readonly IssueDto _issue = new() { Key = "1" };
            private readonly string _user1 = "user1";
            private readonly string _user2 = "user2";

            private IssueChangeLogItemDto CreateItem(IssueChangeLogDto changeLog, string author, string fromValue, string toValue)
            {
                return new IssueChangeLogItemDto
                {
                    Author = author,
                    FromValue = fromValue,
                    ToValue = toValue,
                    ChangeLog = changeLog
                };
            }

            private IssueChangeLogDto CreateChangeLog(DateTime createdDate)
            {
                return new IssueChangeLogDto
                {
                    Issue = _issue,
                    CreatedDate = createdDate
                };
            }

            public IEnumerator GetEnumerator()
            {
                // Element without start
                {
                    var changeLog = CreateChangeLog(new DateTime(2022, 01, 03, 13, 22, 10));
                    var changeLogItem = CreateItem(changeLog, _user1, IssueStatus.InProgress, IssueStatus.InReview);
                    List<IssueChangeLogItemDto> source = new() { changeLogItem };
                    List<RawIssueWorklog> expected = new()
                    {
                        new RawIssueWorklog {
                            Author = changeLogItem.Author,
                            Issue = new Domain.Models.Issues.Issue{ Key = _issue.Key},
                            Source = WorklogSource.Assignee,
                            CompleteDate = changeLog.CreatedDate,
                            StartDate = DateTime.MinValue
                        }
                    };
                    yield return new object[] { source, expected };
                }
                // Element without end
                {
                    var changeLog = CreateChangeLog(new DateTime(2022, 01, 03, 13, 22, 10));
                    var changeLogItem = CreateItem(changeLog, _user1, IssueStatus.Open, IssueStatus.InProgress);
                    List<IssueChangeLogItemDto> source = new() { changeLogItem };
                    List<RawIssueWorklog> expected = new()
                    {
                        new RawIssueWorklog {
                            Author = changeLogItem.Author,
                            Issue = new Domain.Models.Issues.Issue{ Key = _issue.Key},
                            Source = WorklogSource.Assignee,
                            CompleteDate = DateTime.MaxValue,
                            StartDate = changeLog.CreatedDate
                        }
                    };
                    yield return new object[] { source, expected };
                }
                // Element with start and end
                {
                    var changeLogStart = CreateChangeLog(new DateTime(2022, 01, 03, 13, 22, 10));
                    var changeLogItemStart = CreateItem(changeLogStart, _user1, IssueStatus.Open, IssueStatus.InProgress);

                    var changeLogEnd = CreateChangeLog(new DateTime(2022, 01, 03, 17, 41, 10));
                    var changeLogItemEnd = CreateItem(changeLogEnd, _user2, IssueStatus.InProgress, IssueStatus.InReview);

                    List<IssueChangeLogItemDto> source = new() { changeLogItemStart, changeLogItemEnd };
                    List<RawIssueWorklog> expected = new()
                    {
                        new RawIssueWorklog {
                            Author = changeLogItemStart.Author,
                            Issue = new Domain.Models.Issues.Issue{ Key = _issue.Key},
                            Source = WorklogSource.Assignee,
                            CompleteDate = changeLogEnd.CreatedDate,
                            StartDate = changeLogStart.CreatedDate
                        }
                    };
                    yield return new object[] { source, expected };
                }
            }
        }

        [TestCaseSource(typeof(ChangeLogItemConvertToCases))]
        public void ChangeLogItemConvertTo_Should_BeCorrect(List<IssueChangeLogItemDto> source, List<RawIssueWorklog> expected)
        {
            // Arrange
            // Act
            var worklogSource = WorklogSource.Assignee;
			var worklogs = source.ConvertTo<RawIssueWorklog>(IssueStatus.InProgress, _timeProvider, It.IsAny<TimeZoneInfo>(), worklogSource);

            // Assert
            var i = 0;
            var worklogsArray = worklogs.ToArray();
            foreach (var expectedWorklog in expected)
            {
                var worklog = worklogsArray[i];
                Assert.Multiple(() =>
                {
                    Assert.That(worklog.Author, Is.EqualTo(expectedWorklog.Author));
                    Assert.That(worklog.Issue.Key, Is.EqualTo(expectedWorklog.Issue.Key));
                    Assert.That(worklog.Source, Is.EqualTo(worklogSource));
                    Assert.That(worklog.CompleteDate, Is.EqualTo(expectedWorklog.CompleteDate));
                    Assert.That(worklog.StartDate, Is.EqualTo(expectedWorklog.StartDate));
                });
                i++;
            }
        }
    }
}
