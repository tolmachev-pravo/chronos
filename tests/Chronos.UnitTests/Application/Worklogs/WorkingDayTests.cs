using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Worklogs;
using Chronos.UnitTests.Application.Mock;

namespace Chronos.UnitTests.Application.Worklogs
{
    public class WorkingDayTests
    {
        private IIssue[] _issues;
        private DateTime _date;
        private WorkingDaySettings _defaultWorkingDaySettings;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _date = DateTime.Now.Date;
            _defaultWorkingDaySettings = new WorkingDaySettings(
                workingStartTime: TimeSpan.FromHours(9),
                workingEndTime: TimeSpan.FromHours(18),
                lunchTime: TimeSpan.FromHours(1));
            var i = 0;
            _issues = new[]
            {
                new MockIssue($"{i++}"),
                new MockIssue($"{i++}"),
                new MockIssue($"{i++}"),
                new MockIssue($"{i++}"),
            };
        }

        [Test]
        public void Refresh_WithActualAndEstimatedItems_Should_CalculateWorklogsSum()
        {
            // Arrange
            var worklogs = new List<WorkingDayWorklog>
            {
                new WorkingDayWorklog
                {
                    RemainingTimeSpent = TimeSpan.FromHours(3),
                    Type = WorklogType.Actual,
                    StartDate = _date.AddHours(9),
                    CompleteDate = _date.AddHours(12),
                    Issue = _issues[0]
                },
                new WorkingDayWorklog
                {
                    RemainingTimeSpent = TimeSpan.FromHours(4),
                    Type = WorklogType.Estimated,
                    StartDate = _date.AddHours(13),
                    CompleteDate = _date.AddHours(17),
                    Issue = _issues[1]
                }
            };
            var worklogCollectionDay = new WorkingDay(
                date: _date,
                settings: _defaultWorkingDaySettings,
                worklogs: worklogs);

            // Act
            worklogCollectionDay.Refresh();

            // Assert
            Assert.That(worklogCollectionDay.WorklogTimeSpent, Is.EqualTo(worklogCollectionDay.Settings.WorkingTime));
        }

        [Test]
        public void Refresh_WithActualItems_Should_CalculateTimeSpent()
        {
            // Arrange
            var worklogs = new List<WorkingDayWorklog>
            {
                new WorkingDayWorklog
                {
                    RemainingTimeSpent = TimeSpan.FromHours(3),
                    Type = WorklogType.Actual,
                    StartDate = _date.AddHours(9),
                    CompleteDate = _date.AddHours(12),
                    Issue = _issues[0]
                },
                new WorkingDayWorklog
                {
                    RemainingTimeSpent = TimeSpan.FromHours(2),
                    Type = WorklogType.Actual,
                    StartDate = _date.AddHours(13),
                    CompleteDate = _date.AddHours(15),
                    Issue = _issues[1]
                }
            };
            var worklogCollectionDay = new WorkingDay(
                date: _date,
                settings: _defaultWorkingDaySettings,
                worklogs: worklogs);

            // Act
            worklogCollectionDay.Refresh();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(worklogCollectionDay.ActualWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(5)));
                Assert.That(worklogCollectionDay.WorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(5)));
            });
        }

        [Test]
        public void Refresh_Should_SetTimeSpentForEstimatedItems()
        {
            // Arrange
            var worklogs = new List<WorkingDayWorklog>
            {
                new WorkingDayWorklog
                {
                    StartDate = _date.AddHours(10),
                    CompleteDate = _date.AddHours(11),
                    Type = WorklogType.Actual,
                    Issue = _issues[0]
                },
                new WorkingDayWorklog
                {
                    RemainingTimeSpent = TimeSpan.FromHours(2),
                    Type = WorklogType.Estimated,
                    StartDate = _date.AddHours(9),
                    CompleteDate = _date.AddHours(11),
                    Issue = _issues[1]
                },
                new WorkingDayWorklog
                {
                    RemainingTimeSpent = TimeSpan.FromHours(0),
                    Type = WorklogType.Estimated,
                    StartDate = _date.AddHours(12),
                    CompleteDate = _date.AddHours(12),
                    Issue = _issues[2]
                },
            };
            var workingDay = new WorkingDay(
                date: _date,
                settings: _defaultWorkingDaySettings,
                worklogs: worklogs);

            // Act
            workingDay.Refresh();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(workingDay.ActualWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(1)));
                Assert.That(workingDay.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(7)));
                Assert.That(workingDay.WorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(8)));
                Assert.That(workingDay.Progress, Is.EqualTo(12));
            });
        }

        [Test]
        public void Refresh_Should_SetTimeSpentToZero_ForEmptyEstimatedItems()
        {
            // Arrange
            var worklogs = new List<WorkingDayWorklog>
            {
                new WorkingDayWorklog
                {
                    StartDate = _date.AddHours(10),
                    CompleteDate = _date.AddHours(11),
                    Type = WorklogType.Actual,
                    Issue = _issues[0]
                },
                new WorkingDayWorklog
                {
                    RemainingTimeSpent = TimeSpan.FromHours(0),
                    Type = WorklogType.Estimated,
                    Issue = _issues[1]
                },
            };
            var workingDay = new WorkingDay(
                date: _date,
                settings: _defaultWorkingDaySettings,
                worklogs: worklogs);

            // Act
            workingDay.Refresh();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(workingDay.ActualWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(1)));
                Assert.That(workingDay.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.Zero));
                Assert.That(workingDay.WorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(1)));
                Assert.That(workingDay.Progress, Is.EqualTo(100));
            });
        }

        [Test]
        public void Refresh_Should_SetTimeSpentToZero_ForEstimatedWorklog_IfItHasAtLeastOneChild()
        {
            // Arrange
            var worklogs = new List<WorkingDayWorklog>
            {
                // 10:00 - 13:00 | issue_0 | estimated
                new WorkingDayWorklog
                {
                    RemainingTimeSpent = TimeSpan.FromHours(3),
                    Type = WorklogType.Estimated,
                    StartDate = _date.AddHours(10),
                    CompleteDate = _date.AddHours(13),
                    Issue = _issues[0]
                },
                // 13:00 - ..:.. | issue_0 | actual
                new WorkingDayWorklog
                {
                    Type = WorklogType.Actual,
                    StartDate = _date.AddHours(13),
                    CompleteDate = _date.AddHours(15),
                    Issue = _issues[0]
                },
            };
            var workingDay = new WorkingDay(
                date: _date,
                settings: _defaultWorkingDaySettings,
                worklogs: worklogs);

            // Act
            workingDay.Refresh();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(workingDay.ActualWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(2)));
                Assert.That(workingDay.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.Zero));
                Assert.That(workingDay.WorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(2)));
                Assert.That(workingDay.Progress, Is.EqualTo(100));
            });
        }

        [Test]
        public void Refresh_Should_SetRemainingTimeSpent_ForEstimatedWorklogs_WithoutChildren()
        {
            // Arrange
            var worklogs = new List<WorkingDayWorklog>
            {
                // 10:00 - 13:00 | issue_0 | estimated
                new WorkingDayWorklog
                {
                    RemainingTimeSpent = TimeSpan.FromHours(3),
                    Type = WorklogType.Estimated,
                    StartDate = _date.AddHours(10),
                    CompleteDate = _date.AddHours(13),
                    Issue = _issues[0]
                },
                // 13:00 - ..:.. | issue_0 | actual
                new WorkingDayWorklog
                {
                    Type = WorklogType.Actual,
                    StartDate = _date.AddHours(13),
                    CompleteDate = _date.AddHours(15),
                    Issue = _issues[0]
                },
                // 13:00 - 17:00 | issue_1 | estimated
                new WorkingDayWorklog
                {
                    RemainingTimeSpent = TimeSpan.FromHours(4),
                    Type = WorklogType.Estimated,
                    StartDate = _date.AddHours(13),
                    CompleteDate = _date.AddHours(17),
                    Issue = _issues[1]
                },
            };
            var workingDay = new WorkingDay(
                date: _date,
                settings: _defaultWorkingDaySettings,
                worklogs: worklogs);

            // Act
            workingDay.Refresh();

            // Assert
            var issue0EstimatedWorklog = workingDay.EstimatedWorklogs.First(worklog => worklog.Issue.Key == _issues[0].Key);
            var issue1EstimatedWorklog = workingDay.EstimatedWorklogs.First(worklog => worklog.Issue.Key == _issues[1].Key);
            Assert.Multiple(() =>
            {
                Assert.That(workingDay.ActualWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(2)));
                Assert.That(workingDay.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(6)));
                Assert.That(workingDay.WorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(8)));
                Assert.That(workingDay.Progress, Is.EqualTo(25));
                Assert.That(issue0EstimatedWorklog.RemainingTimeSpent, Is.EqualTo(TimeSpan.Zero));
                Assert.That(issue0EstimatedWorklog.ChildrenTimeSpent, Is.EqualTo(TimeSpan.FromHours(2)));
                Assert.That(issue1EstimatedWorklog.RemainingTimeSpent, Is.EqualTo(TimeSpan.FromHours(6)));
                Assert.That(issue1EstimatedWorklog.ChildrenTimeSpent, Is.EqualTo(TimeSpan.Zero));
            });            
        }

        [Test]
        public void Refresh_Should_SetTimeSpentInProportions()
        {
            // Arrange
            var worklogs = new List<WorkingDayWorklog>
            {
                // 10:00 - 13:45 | issue_0 | estimated
                new WorkingDayWorklog
                {
                    RemainingTimeSpent =  new TimeSpan(3, 45, 0),
                    Type = WorklogType.Estimated,
                    StartDate = _date.AddHours(10),
                    CompleteDate = _date.AddHours(13).AddMinutes(45),
                    Issue = _issues[0]
                },
                // 14:00 - 16:15 | issue_1 | estimated
                new WorkingDayWorklog
                {
                    RemainingTimeSpent = new TimeSpan(2, 15, 0),
                    Type = WorklogType.Estimated,
                    StartDate = _date.AddHours(14),
                    CompleteDate = _date.AddHours(16).AddMinutes(15),
                    Issue = _issues[1]
                },
            };
            var workingDay = new WorkingDay(
                date: _date,
                settings: _defaultWorkingDaySettings,
                worklogs: worklogs);

            // Act
            workingDay.Refresh();

            // Assert
            var issue0EstimatedWorklog = workingDay.EstimatedWorklogs.First(worklog => worklog.Issue.Key == _issues[0].Key);
            var issue1EstimatedWorklog = workingDay.EstimatedWorklogs.First(worklog => worklog.Issue.Key == _issues[1].Key);
            Assert.Multiple(() =>
            {
                Assert.That(workingDay.ActualWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(0)));
                Assert.That(workingDay.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(8)));
                Assert.That(workingDay.WorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(8)));
                Assert.That(workingDay.Progress, Is.EqualTo(0));
                Assert.That(issue0EstimatedWorklog.RemainingTimeSpent, Is.EqualTo(TimeSpan.FromHours(5)));
                Assert.That(issue1EstimatedWorklog.RemainingTimeSpent, Is.EqualTo(TimeSpan.FromHours(3)));
            });
        }

        [Test]
        public void Refresh_CalendarEvent_Unmatched_ShouldUseOwnTimeSpent()
        {
            // Arrange — calendar event with key, within working hours
            var worklogs = new List<WorkingDayWorklog>
            {
                new WorkingDayWorklog
                {
                    RawStartDate = _date.AddHours(10),
                    RawCompleteDate = _date.AddHours(11).AddMinutes(30),
                    StartDate = _date.AddHours(10),
                    CompleteDate = _date.AddHours(11).AddMinutes(30),
                    Type = WorklogType.Estimated,
                    Source = EventSource.Calendar,
                    Issue = _issues[0]
                }
            };
            var workingDay = new WorkingDay(_date, _defaultWorkingDaySettings, worklogs);

            // Act
            workingDay.Refresh();

            // Assert — direct own time, not scaled to day capacity
            Assert.That(
                workingDay.EstimatedWorklogs.First().RemainingTimeSpent,
                Is.EqualTo(new TimeSpan(1, 30, 0)));
        }

        [Test]
        public void Refresh_CalendarEvent_OutsideWorkingHours_ShouldUseRawTimeSpent()
        {
            // Arrange — event 8:00-9:30, working day starts at 9:00 → clipped StartDate=9:00, TimeSpent=30m
            // but RawTimeSpent=1h30m and that is what should be used
            var worklogs = new List<WorkingDayWorklog>
            {
                new WorkingDayWorklog
                {
                    RawStartDate = _date.AddHours(8),
                    RawCompleteDate = _date.AddHours(9).AddMinutes(30),
                    StartDate = _date.AddHours(9),          // clipped to working start
                    CompleteDate = _date.AddHours(9).AddMinutes(30),
                    Type = WorklogType.Estimated,
                    Source = EventSource.Calendar,
                    Issue = _issues[0]
                }
            };
            var workingDay = new WorkingDay(_date, _defaultWorkingDaySettings, worklogs);

            // Act
            workingDay.Refresh();

            // Assert — raw 1h30m, not clipped 30m
            Assert.That(
                workingDay.EstimatedWorklogs.First().RemainingTimeSpent,
                Is.EqualTo(new TimeSpan(1, 30, 0)));
        }

        [Test]
        public void Refresh_CommentEvent_Unmatched_ShouldUseOwnTimeSpent()
        {
            // Arrange
            var worklogs = new List<WorkingDayWorklog>
            {
                new WorkingDayWorklog
                {
                    RawStartDate = _date.AddHours(14),
                    RawCompleteDate = _date.AddHours(14).AddMinutes(45),
                    StartDate = _date.AddHours(14),
                    CompleteDate = _date.AddHours(14).AddMinutes(45),
                    Type = WorklogType.Estimated,
                    Source = EventSource.Comment,
                    Issue = _issues[0]
                }
            };
            var workingDay = new WorkingDay(_date, _defaultWorkingDaySettings, worklogs);

            // Act
            workingDay.Refresh();

            // Assert
            Assert.That(
                workingDay.EstimatedWorklogs.First().RemainingTimeSpent,
                Is.EqualTo(TimeSpan.FromMinutes(45)));
        }

        [Test]
        public void Refresh_AssigneeAndComment_Unmatched_CommentReducesProportionalPool()
        {
            // Arrange
            // Day: 8h. Comment fixed=1h → proportional pool = 8h - 1h = 7h
            // Assignee 4h proposed → gets all 7h (only proportional event)
            var worklogs = new List<WorkingDayWorklog>
            {
                new WorkingDayWorklog
                {
                    RawStartDate = _date.AddHours(9),
                    RawCompleteDate = _date.AddHours(13),
                    StartDate = _date.AddHours(9),
                    CompleteDate = _date.AddHours(13),
                    Type = WorklogType.Estimated,
                    Source = EventSource.Assignee,
                    Issue = _issues[0]
                },
                new WorkingDayWorklog
                {
                    RawStartDate = _date.AddHours(13),
                    RawCompleteDate = _date.AddHours(14),
                    StartDate = _date.AddHours(13),
                    CompleteDate = _date.AddHours(14),
                    Type = WorklogType.Estimated,
                    Source = EventSource.Comment,
                    Issue = _issues[1]
                }
            };
            var workingDay = new WorkingDay(_date, _defaultWorkingDaySettings, worklogs);

            // Act
            workingDay.Refresh();

            // Assert
            var assigneeWorklog = workingDay.EstimatedWorklogs.First(w => w.Issue.Key == _issues[0].Key);
            var commentWorklog = workingDay.EstimatedWorklogs.First(w => w.Issue.Key == _issues[1].Key);
            Assert.Multiple(() =>
            {
                Assert.That(commentWorklog.RemainingTimeSpent, Is.EqualTo(TimeSpan.FromHours(1)));
                Assert.That(assigneeWorklog.RemainingTimeSpent, Is.EqualTo(TimeSpan.FromHours(7)));
                Assert.That(workingDay.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(8)));
            });
        }

        [Test]
        public void Refresh_CalendarAndAssignee_WithActual_CalendarMatchedGoesToZero()
        {
            // Arrange
            // Calendar event matched to actual → 0; Assignee unmatched → proportional from remaining
            var worklogs = new List<WorkingDayWorklog>
            {
                // calendar estimated 10:00-11:00, matched to actual below
                new WorkingDayWorklog
                {
                    Type = WorklogType.Estimated,
                    Source = EventSource.Calendar,
                    StartDate = _date.AddHours(10),
                    CompleteDate = _date.AddHours(11),
                    Issue = _issues[0]
                },
                // actual matching the calendar event
                new WorkingDayWorklog
                {
                    Type = WorklogType.Actual,
                    Source = EventSource.Calendar,
                    StartDate = _date.AddHours(10),
                    CompleteDate = _date.AddHours(11),
                    Issue = _issues[0]
                },
                // assignee estimated 11:00-13:00 = 2h, unmatched
                new WorkingDayWorklog
                {
                    Type = WorklogType.Estimated,
                    Source = EventSource.Assignee,
                    StartDate = _date.AddHours(11),
                    CompleteDate = _date.AddHours(13),
                    Issue = _issues[1]
                }
            };
            var workingDay = new WorkingDay(_date, _defaultWorkingDaySettings, worklogs);

            // Act
            workingDay.Refresh();

            // Assert
            var calendarWorklog = workingDay.EstimatedWorklogs.First(w => w.Issue.Key == _issues[0].Key);
            var assigneeWorklog = workingDay.EstimatedWorklogs.First(w => w.Issue.Key == _issues[1].Key);
            Assert.Multiple(() =>
            {
                Assert.That(calendarWorklog.RemainingTimeSpent, Is.EqualTo(TimeSpan.Zero));
                // remainingDayTimeSpent = 8h - 1h (actual) - 0 (calBlocked) - 0 (no unmatched fixed) = 7h
                Assert.That(assigneeWorklog.RemainingTimeSpent, Is.EqualTo(TimeSpan.FromHours(7)));
                Assert.That(workingDay.ActualWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(1)));
                Assert.That(workingDay.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(7)));
            });
        }
    }
}
