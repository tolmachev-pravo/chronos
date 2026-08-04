using Chronos.Application.Issues;
using Chronos.Application.Issues.Queries;
using Chronos.Domain.Models.Issues;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Mock
{
    internal class MockIssueDataSource : IIssueDataSource
    {
		public async Task<Issue> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default)
		{
			return await Task.FromResult(IssueGenerator.Create(issueKey));
		}

		public async Task<string> GetIssueOpenPullRequestUrlAsync(GetIssueOpenPullRequestUrl.Query query, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult("https://github.com");
        }

        public async Task<IEnumerable<IssueStatus>> GetIssueStatusesAsync(GetIssueStatuses.Query query, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(MockIssueStorage.IssueStatuses);
        }
    }
}
