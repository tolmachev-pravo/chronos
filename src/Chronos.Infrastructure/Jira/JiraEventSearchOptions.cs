using Atlassian.Jira;

namespace Chronos.Infrastructure.Jira
{
    /// <summary>
    /// Search options shared by the Jira event providers. The events only need the issue
    /// summary for display; the heavy work is the per-issue changelog/comment fetch that
    /// follows. Restricting the search to the summary field keeps the initial JQL
    /// response small instead of pulling every navigable field. See issue #258.
    /// </summary>
    public static class JiraEventSearchOptions
    {
        public static IssueSearchOptions Create(string jql) =>
            new(jql)
            {
                MaxIssuesPerRequest = JiraConstants.DefaultMaxIssuesPerRequest,
                FetchBasicFields = false,
                AdditionalFields = { "summary" }
            };
    }
}
