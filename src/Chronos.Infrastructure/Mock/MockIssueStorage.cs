using Chronos.Domain.Models.Issues;
using Chronos.Infrastructure.Jira;
using System.Collections.Generic;

namespace Chronos.Infrastructure.Mock
{
    internal static class MockIssueStorage
    {
        public static IEnumerable<IssueStatus> IssueStatuses = new List<IssueStatus>
        {
            JiraConstants.Status.Default,
            new IssueStatus { Id = "1", Name = "Open" },
            new IssueStatus { Id = "2", Name = "Reopened" },
            new IssueStatus { Id = "4", Name = "Wait" },
            new IssueStatus { Id = "5", Name = "Done" },
			new IssueStatus { Id = "10116", Name = "In Testing" }
		};
    }
}
