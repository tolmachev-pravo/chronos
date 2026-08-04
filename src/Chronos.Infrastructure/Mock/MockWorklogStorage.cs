using Chronos.Domain.Models.Issues;
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

        public static IList<IWorklog> RawIssueWorklogs = new List<IWorklog>
        {
            new RawIssueWorklog
            {
                StartDate = DateTime.Now.Date.AddHours(10),
                CompleteDate = DateTime.Now.Date.AddHours(11),
                Issue = Issues[0]
            },
            new RawIssueWorklog
            {
                StartDate = DateTime.Now.Date.AddHours(11),
                CompleteDate = DateTime.Now.Date.AddHours(17),
                Issue = Issues[1]
            },
            new RawIssueWorklog
            {
                StartDate = DateTime.Now.Date.AddHours(17),
                CompleteDate = DateTime.Now.Date.AddHours(19),
                Issue = Issues[2]
            },
            new RawIssueWorklog
            {
                StartDate = DateTime.Now.Date.AddDays(-1).AddHours(11),
                CompleteDate = DateTime.Now.Date.AddDays(-1).AddHours(16),
                Issue = Issues[1]
            },
            new RawIssueWorklog
            {
                StartDate = DateTime.Now.Date.AddDays(-1).AddHours(16),
                CompleteDate = DateTime.Now.Date.AddDays(-1).AddHours(20),
                Issue = Issues[3]
            },
            new RawIssueWorklog
            {
                StartDate = DateTime.Now.Date.AddDays(-5).AddHours(19),
                CompleteDate = DateTime.Now.Date.AddDays(-2).AddHours(19),
                Issue = Issues[4]
            },            
            new RawIssueWorklog
            {
                StartDate = DateTime.Now.Date.AddDays(-3).AddHours(11),
                CompleteDate = DateTime.Now.Date.AddDays(-3).AddHours(12),
                Issue = Issues[5],
                Source = WorklogSource.Comment
            },
            new RawIssueWorklog
			{
				StartDate = DateTime.Now.Date.AddDays(-3).AddHours(12),
				CompleteDate = DateTime.Now.Date.AddDays(-3).AddHours(15),
				Issue = Issues[6],
				Source = WorklogSource.Tester
			}
		};
    }
}
