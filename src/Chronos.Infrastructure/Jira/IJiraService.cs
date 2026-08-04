using Atlassian.Jira;
using Chronos.Application.Authentication;
using Chronos.Application.Worklogs.Dto;
using Chronos.Infrastructure.Jira.Dto;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Jira
{
    public interface IJiraService
    {
        Task<Dictionary<string, IssueDto>> GetIssuesAsync(
            string[] issueKeys,
            CancellationToken cancellationToken = default);

		Task<IssueDto> GetIssueAsync(
            string issueKey,
            CancellationToken cancellationToken = default);

		Task<IEnumerable<IssueDto>> GetIssuesAsync(
            IssueSearchOptions issueSearchOptions,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<IssueWorklogDto>> GetIssueWorklogsAsync(
            IssueSearchOptions issueSearchOptions,
            Func<Worklog, bool> worklogFilter = null,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<IssueWorklogDto>> GetIssueWorklogsAsync(
            IEnumerable<IssueDto> issues,
            Func<Worklog, bool> worklogFilter = null,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<IssueChangeLogDto>> GetIssueChangeLogsAsync(
            IEnumerable<IssueDto> issues,
            Func<IssueChangeLog, bool> changeLogFilter = null,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<IssueChangeLogItemDto>> GetIssueChangeLogItemsAsync(
            IEnumerable<IssueDto> issues,
            Func<IssueChangeLog, bool> changeLogFilter = null,
            Func<IssueChangeLogItem, bool> changeLogItemFilter = null,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<IssueCommentDto>> GetIssueCommentsAsync(
            IEnumerable<IssueDto> issues,
            Func<Comment, bool> filter = null,
            CancellationToken cancellationToken = default);

        Task<UserDto> GetCurrentUserAsync(
            CancellationToken cancellationToken = default);

        Task<LoginResponse> LoginAsync(
            BasicLoginRequest request,
            CancellationToken cancellationToken = default);

        Task<LoginResponse> LoginAsync(
            BearerLoginRequest request,
            CancellationToken cancellationToken = default);

        Task AddWorklogAsync(
            AddedWorklogDto worklog,
            CancellationToken cancellationToken = default);

        Task<string> GetCurrentUserAvatarAsync(
            CancellationToken cancellationToken = default);

        Task<IEnumerable<IssueStatusDto>> GetIssueStatusesAsync(
            CancellationToken cancellationToken = default);

        Task<HttpStatusCode> PingAsync(
            CancellationToken cancellationToken = default);

        Task<DevStatusDetailDto> GetIssueDevStatusDetailAsync(string jiraIdentifier, string applicationType = "github",
           string dataType = "pullrequest", CancellationToken cancellationToken = default);

        Task<IEnumerable<TempoWorklogDto>> GetTempoWorklogsAsync(
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default);

        Task<TimeSpan> GetServerUtcOffsetAsync(
            CancellationToken cancellationToken = default);
    }
}
