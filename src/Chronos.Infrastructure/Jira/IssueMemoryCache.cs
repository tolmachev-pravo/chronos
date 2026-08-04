using Microsoft.Extensions.Options;
using Chronos.Application.Common.Extensions;
using Chronos.Application.Storage;
using Chronos.Domain.Models.Issues;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Jira
{
    public class IssueMemoryCache : BaseMemoryCache<string, Issue>
    {
        private readonly IJiraConfiguration _jiraConfiguration;
        private readonly IJiraService _jiraService;

        public IssueMemoryCache(
            IOptions<JiraConfiguration> jiraConfiguration,
            IJiraService jiraService)
        {
            _jiraConfiguration = jiraConfiguration.Value;
            _jiraService = jiraService;
        }

        public override async Task<ConcurrentDictionary<string, Issue>> GetValuesAsync()
        {
            var storage = await base.GetValuesAsync();
            if (storage.IsEmpty() && !_jiraConfiguration.CachedIssues.IsEmpty())
            {
                var issues = await _jiraService.GetIssuesAsync(_jiraConfiguration.CachedIssues);
				foreach (var issue in issues)
                {
                    TryAdd(issue.Value.Adapt());
                }
            }
            return await base.GetValuesAsync();
        }
    }
}
