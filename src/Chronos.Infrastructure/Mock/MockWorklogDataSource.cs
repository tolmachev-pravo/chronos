using Chronos.Application.Worklogs;
using Chronos.Application.Worklogs.Queries;
using Chronos.Domain.Models.Worklogs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Mock
{
    internal class MockWorklogDataSource : IWorklogDataSource
    {
        public Task<IEnumerable<IWorklog>> GetIssueWorklogsAsync(GetIssueWorklogs.Query query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MockWorklogStorage.IssueWorklogs.AsEnumerable());
        }
    }
}
