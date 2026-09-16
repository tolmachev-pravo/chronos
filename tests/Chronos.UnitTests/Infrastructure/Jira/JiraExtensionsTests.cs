using Moq;
using Chronos.Application.Time;
using Chronos.Domain.Models.Events;
using Chronos.Infrastructure.Jira;
using Chronos.Infrastructure.Jira.Dto;
using Microsoft.Extensions.Options;
using System.Collections;

namespace Chronos.UnitTests.Infrastructure.Jira
{
    [TestFixture]
    public class JiraExtensionsTests
    {
        private ITimeProvider _timeProvider;
        private IJiraLinkGenerator _linkGenerator;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var timeProviderMock = new Mock<ITimeProvider>();
            timeProviderMock
                .Setup(mock => mock.ConvertToUserTimezone(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()))
                .Returns((DateTime dateTime, TimeZoneInfo userTimeZone) => dateTime);
            _timeProvider = timeProviderMock.Object;
            _linkGenerator = new JiraLinkGenerator(
                Options.Create(new JiraConfiguration { Url = "https://jira.test" }));
        }

        class CommentConvertToCases : IEnumerable
        {
            public IEnumerator GetEnumerator()
            {
                {
                    yield return new object[] {
                        new IssueCommentDto
                        {
                            Id = "42",
                            CreatedDate = new DateTime(2022, 01, 03, 13, 22, 10),
                            Author = "user1",
                            AuthorDisplayName = "User One",
                            Body = "looks good to me",
                            Issue = new IssueDto { Key = "CASEM-1" }
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
            var worklogs = comments.ConvertTo(
                _timeProvider, It.IsAny<TimeZoneInfo>(), EventSource.Comment, TimeSpan.FromMinutes(10), _linkGenerator);

            // Assert
            var worklog = worklogs.Single();
            Assert.Multiple(() =>
            {
                Assert.That(worklog.Author, Is.EqualTo(comment.Author));
                Assert.That(worklog.Duration, Is.EqualTo(TimeSpan.FromMinutes(10)));
                Assert.That(worklog.CompleteDate, Is.EqualTo(comment.CreatedDate));
                Assert.That(worklog.StartDate, Is.EqualTo(comment.CreatedDate.AddMinutes(-10)));
                Assert.That(worklog.Source, Is.EqualTo(EventSource.Comment));
            });

            // The comment itself travels with the event. See issue #156.
            var details = worklog.Details as CommentEventDetails;
            Assert.That(details, Is.Not.Null);
            Assert.That(details.Body, Is.EqualTo(comment.Body));
            Assert.That(details.Author, Is.EqualTo(comment.AuthorDisplayName));
            Assert.That(details.Link, Does.Contain("focusedCommentId=42"));
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
                    List<UserEvent> expected = new()
                    {
                        new UserEvent {
                            Author = changeLogItem.Author,
                            Issue = new Domain.Models.Issues.Issue{ Key = _issue.Key},
                            Source = EventSource.Assignee,
                            CompleteDate = changeLog.CreatedDate,
                            StartDate = DateTime.MinValue,
                            // Entering In Progress predates the changelog, so only the
                            // status it left for is known. See issue #156.
                            Details = new TransitionEventDetails
                            {
                                Status = IssueStatus.InProgress,
                                ToStatus = IssueStatus.InReview
                            }
                        }
                    };
                    yield return new object[] { source, expected };
                }
                // Element without end
                {
                    var changeLog = CreateChangeLog(new DateTime(2022, 01, 03, 13, 22, 10));
                    var changeLogItem = CreateItem(changeLog, _user1, IssueStatus.Open, IssueStatus.InProgress);
                    List<IssueChangeLogItemDto> source = new() { changeLogItem };
                    List<UserEvent> expected = new()
                    {
                        new UserEvent {
                            Author = changeLogItem.Author,
                            Issue = new Domain.Models.Issues.Issue{ Key = _issue.Key},
                            Source = EventSource.Assignee,
                            CompleteDate = DateTime.MaxValue,
                            StartDate = changeLog.CreatedDate,
                            // Never left the status, so there is no status it went to.
                            Details = new TransitionEventDetails
                            {
                                Status = IssueStatus.InProgress,
                                FromStatus = IssueStatus.Open
                            }
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
                    List<UserEvent> expected = new()
                    {
                        new UserEvent {
                            Author = changeLogItemStart.Author,
                            Issue = new Domain.Models.Issues.Issue{ Key = _issue.Key},
                            Source = EventSource.Assignee,
                            CompleteDate = changeLogEnd.CreatedDate,
                            StartDate = changeLogStart.CreatedDate,
                            Details = new TransitionEventDetails
                            {
                                Status = IssueStatus.InProgress,
                                FromStatus = IssueStatus.Open,
                                ToStatus = IssueStatus.InReview
                            }
                        }
                    };
                    yield return new object[] { source, expected };
                }
            }
        }

        [TestCaseSource(typeof(ChangeLogItemConvertToCases))]
        public void ChangeLogItemConvertTo_Should_BeCorrect(List<IssueChangeLogItemDto> source, List<UserEvent> expected)
        {
            // Arrange
            // Act
            var worklogSource = EventSource.Assignee;
			var worklogs = source.ConvertTo(IssueStatus.InProgress, _timeProvider, It.IsAny<TimeZoneInfo>(), worklogSource);

            // Assert
            var i = 0;
            var worklogsArray = worklogs.ToArray();
            foreach (var expectedWorklog in expected)
            {
                var worklog = worklogsArray[i];
                Assert.Multiple(() =>
                {
                    Assert.That(worklog.Author, Is.EqualTo(expectedWorklog.Author));
                    Assert.That(worklog.Issue!.Key, Is.EqualTo(expectedWorklog.Issue!.Key));
                    Assert.That(worklog.Source, Is.EqualTo(worklogSource));
                    Assert.That(worklog.CompleteDate, Is.EqualTo(expectedWorklog.CompleteDate));
                    Assert.That(worklog.StartDate, Is.EqualTo(expectedWorklog.StartDate));
                });

                // The transition the event stands for. See issue #156.
                var details = worklog.Details as TransitionEventDetails;
                var expectedDetails = (TransitionEventDetails)expectedWorklog.Details;
                Assert.That(details, Is.Not.Null);
                Assert.Multiple(() =>
                {
                    Assert.That(details.Status, Is.EqualTo(expectedDetails.Status));
                    Assert.That(details.FromStatus, Is.EqualTo(expectedDetails.FromStatus));
                    Assert.That(details.ToStatus, Is.EqualTo(expectedDetails.ToStatus));
                });
                i++;
            }
        }
    }
}
