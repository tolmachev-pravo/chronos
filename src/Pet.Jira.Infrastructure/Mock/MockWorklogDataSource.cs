using Pet.Jira.Application.Worklogs;
using Pet.Jira.Application.Worklogs.Queries;
using Pet.Jira.Domain.Models.Worklogs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Infrastructure.Mock
{
    internal class MockWorklogDataSource : IWorklogDataSource
    {
        public Task<IEnumerable<IWorklog>> GetIssueWorklogsAsync(GetIssueWorklogs.Query query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MockWorklogStorage.IssueWorklogs.AsEnumerable());
        }

        public Task<IEnumerable<IWorklog>> GetAssigneeRawIssueWorklogsAsync(GetAssigneeJiraEvents.Query query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FilterBySource(WorklogSource.Assignee));
        }

        public Task<IEnumerable<IWorklog>> GetTesterRawIssueWorklogsAsync(GetTesterJiraEvents.Query query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FilterBySource(WorklogSource.Tester));
        }

        public Task<IEnumerable<IWorklog>> GetCommentRawIssueWorklogsAsync(GetCommentJiraEvents.Query query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FilterBySource(WorklogSource.Comment));
        }

        private static IEnumerable<IWorklog> FilterBySource(WorklogSource source)
        {
            return MockWorklogStorage.RawIssueWorklogs
                .Where(worklog => worklog is RawIssueWorklog raw && raw.Source == source)
                .ToList();
        }
    }
}
