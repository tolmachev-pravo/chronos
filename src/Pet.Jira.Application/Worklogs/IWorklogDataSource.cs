using Pet.Jira.Application.Worklogs.Queries;
using Pet.Jira.Domain.Models.Worklogs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Application.Worklogs
{
    public interface IWorklogDataSource
    {
        Task<IEnumerable<IWorklog>> GetIssueWorklogsAsync(
            GetIssueWorklogs.Query query,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<IWorklog>> GetAssigneeRawIssueWorklogsAsync(
            GetAssigneeJiraEvents.Query query,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<IWorklog>> GetTesterRawIssueWorklogsAsync(
            GetTesterJiraEvents.Query query,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<IWorklog>> GetCommentRawIssueWorklogsAsync(
            GetCommentJiraEvents.Query query,
            CancellationToken cancellationToken = default);
    }
}
