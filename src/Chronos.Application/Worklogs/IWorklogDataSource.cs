using Chronos.Application.Worklogs.Queries;
using Chronos.Domain.Models.Worklogs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Worklogs
{
    /// <summary>
    /// The source of real time entries. Everything that is merely a trace of activity
    /// is served by <see cref="Events.IEventDataSource"/> instead. See issue #299.
    /// </summary>
    public interface IWorklogDataSource
    {
        Task<IEnumerable<IWorklog>> GetIssueWorklogsAsync(
            GetIssueWorklogs.Query query,
            CancellationToken cancellationToken = default);
    }
}
