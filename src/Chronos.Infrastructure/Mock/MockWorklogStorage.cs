using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;

namespace Chronos.Infrastructure.Mock
{
    internal static class MockWorklogStorage
    {
        public static IList<Issue> Issues = new List<Issue>
        {
            IssueGenerator.Create(),
            IssueGenerator.Create(),
            IssueGenerator.Create(),
            IssueGenerator.Create(),
            IssueGenerator.Create(),
            IssueGenerator.Create(),
            IssueGenerator.Create(),
            IssueGenerator.Create(),
            IssueGenerator.Create()
        };

        public static IssueWorklog CreateIssueWorklog(
            DateTime startTime,
            TimeSpan duration,
            Issue issue)
        {
            return new IssueWorklog
            {
                StartDate = startTime,
                CompleteDate = startTime.Add(duration),
                TimeSpent = duration,
                Issue = issue
            };
        }

        public static IList<IWorklog> IssueWorklogs = new List<IWorklog>
        {
            CreateIssueWorklog(
                startTime: DateTime.Now.Date.AddHours(10),
                duration: TimeSpan.FromHours(1),
                issue: Issues[0]),
            CreateIssueWorklog(
                startTime: DateTime.Now.Date.AddHours(9).AddMinutes(30),
                duration: TimeSpan.FromMinutes(30),
                issue: Issues[0]),
            CreateIssueWorklog(
                startTime: DateTime.Now.Date.AddHours(13),
                duration: TimeSpan.FromHours(2),
                issue: Issues[7]),
            CreateIssueWorklog(
                startTime: DateTime.Now.Date.AddDays(-1).AddHours(11),
                duration: TimeSpan.FromHours(5),
                issue: Issues[1]),
            CreateIssueWorklog(
                startTime: DateTime.Now.Date.AddDays(-2).AddHours(11),
                duration: TimeSpan.FromHours(8),
                issue: Issues[4])
        };

        public static IList<UserEvent> Events = new List<UserEvent>
        {
            new UserEvent
            {
                StartDate = DateTime.Now.Date.AddHours(10),
                CompleteDate = DateTime.Now.Date.AddHours(11),
                Issue = Issues[0],
                Details = Transition("Open", "In Review")
            },
            new UserEvent
            {
                StartDate = DateTime.Now.Date.AddHours(11),
                CompleteDate = DateTime.Now.Date.AddHours(17),
                Issue = Issues[1],
                // Still in progress at the end of the period — the row has to cope with
                // half a transition. See issue #156.
                Details = Transition("Backlog", null)
            },
            // No details at all: the source icon stays a plain icon here.
            new UserEvent
            {
                StartDate = DateTime.Now.Date.AddHours(17),
                CompleteDate = DateTime.Now.Date.AddHours(19),
                Issue = Issues[2]
            },
            new UserEvent
            {
                StartDate = DateTime.Now.Date.AddDays(-1).AddHours(11),
                CompleteDate = DateTime.Now.Date.AddDays(-1).AddHours(16),
                Issue = Issues[1]
            },
            new UserEvent
            {
                StartDate = DateTime.Now.Date.AddDays(-1).AddHours(16),
                CompleteDate = DateTime.Now.Date.AddDays(-1).AddHours(20),
                Issue = Issues[3]
            },
            new UserEvent
            {
                StartDate = DateTime.Now.Date.AddDays(-5).AddHours(19),
                CompleteDate = DateTime.Now.Date.AddDays(-2).AddHours(19),
                Issue = Issues[4]
            },            
            new UserEvent
            {
                StartDate = DateTime.Now.Date.AddDays(-3).AddHours(11),
                CompleteDate = DateTime.Now.Date.AddDays(-3).AddHours(12),
                Issue = Issues[5],
                Source = EventSource.Comment,
                Details = new CommentEventDetails
                {
                    Author = "Анна Ковалёва",
                    Body = "Сертификат на стенде протухает 20-го, я продлил его вручную."
                        + Environment.NewLine + Environment.NewLine
                        + "В пайплайне он всё ещё берётся из старого секрета, так что после "
                        + "следующего деплоя всё вернётся — заведите отдельную задачу.",
                    Link = Issues[5].Link
                }
            },
            new UserEvent
			{
				StartDate = DateTime.Now.Date.AddDays(-3).AddHours(12),
				CompleteDate = DateTime.Now.Date.AddDays(-3).AddHours(15),
				Issue = Issues[6],
				Source = EventSource.Tester,
                Details = Transition("In Review", "Done")
			},
            new UserEvent
            {
                StartDate = DateTime.Now.Date.AddHours(9),
                CompleteDate = DateTime.Now.Date.AddHours(9).AddMinutes(30),
                Issue = Issues[8],
                Source = EventSource.Calendar,
                Summary = "Груминг бэклога",
                Details = new CalendarEventDetails
                {
                    Title = "Груминг бэклога",
                    Organizer = "Анна Ковалёва",
                    Location = "Переговорка «Сатурн» · Telemost",
                    Description = "Разбираем очередь на спринт.",
                    Attendees = new[] { "Анна Ковалёва", "Дмитрий Толмачёв", "Пётр Иванов" }
                }
            },
            // A meeting with no issue key: it blocks day time and cannot be logged, and
            // its own details are the only thing that says what the hour was.
            new UserEvent
            {
                StartDate = DateTime.Now.Date.AddHours(19),
                CompleteDate = DateTime.Now.Date.AddHours(20),
                Source = EventSource.Calendar,
                Summary = "Ретро спринта",
                Details = new CalendarEventDetails
                {
                    Title = "Ретро спринта",
                    Organizer = "Анна Ковалёва",
                    Location = "Telemost"
                }
            }
		};

        private static TransitionEventDetails Transition(string from, string to) =>
            new()
            {
                Status = "In Progress",
                FromStatus = from,
                ToStatus = to
            };
    }
}
