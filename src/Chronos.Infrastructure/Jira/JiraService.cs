using Atlassian.Jira;
using Atlassian.Jira.Remote;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Chronos.Application.Authentication;
using Chronos.Application.Common.Extensions;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Users;
using Chronos.Infrastructure.Jira.Dto;
using RestSharp.Authenticators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Jira
{
    public class JiraService : IJiraService
    {
        private readonly IJiraLinkGenerator _linkGenerator;
        private readonly ILogger<JiraService> _logger;
        private readonly Atlassian.Jira.Jira _jiraClient;
        private readonly IJiraConfiguration _config;
        private readonly User _user;

        private static ParallelOptions DefaultParallelOptions =>
            new() { MaxDegreeOfParallelism = (int)Math.Round(Environment.ProcessorCount * 0.8) };

        // The Jira server offset is process-global (single instance), so cache it across
        // requests and users. Benign races only cause a couple of extra fetches.
        private static TimeSpan? _cachedServerUtcOffset;
        private static readonly SemaphoreSlim ServerUtcOffsetLock = new(1, 1);

        public JiraService(
            IOptions<JiraConfiguration> jiraConfiguration,
            IJiraLinkGenerator linkGenerator,
            IIdentityService identityService,
            ILogger<JiraService> logger)
        {
            _linkGenerator = linkGenerator;
            _logger = logger;
            _config = jiraConfiguration.Value;
            _user = identityService.CurrentUser;
            _jiraClient = _user?.PersonalAccessToken != null
                ? CreateBearerRestClient(_config.Url, _user.PersonalAccessToken)
                : Atlassian.Jira.Jira.CreateRestClient(_config.Url, _user?.Username, _user?.Password);
        }

        /// <summary>
        /// Get issues with pagination
        /// </summary>
        /// <param name="issueSearchOptions"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<IssueDto>> GetPaginationIssuesAsync(
            IssueSearchOptions issueSearchOptions,
            CancellationToken cancellationToken = default)
        {
            int itemsPerPage = 10;
            int startAt = 0;

            var issues = new ConcurrentBag<IssueDto>();
            while (true)
            {
                var result = await _jiraClient.Issues.GetIssuesFromJqlAsync(issueSearchOptions.Jql, itemsPerPage, startAt, cancellationToken);
                if (!result.Any())
                {
                    break;
                }

                foreach (var issue in result)
                {
                    issues.Add(IssueDto.Create(issue, _linkGenerator));
                }

                startAt += itemsPerPage;
            }
            return issues;
        }

        /// <summary>
        /// Get issues
        /// </summary>
        /// <param name="issueKeys"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, IssueDto>> GetIssuesAsync(
            string[] issueKeys,
            CancellationToken cancellationToken = default)
        {
            var issues = await _jiraClient.Issues.GetIssuesAsync(issueKeys);
            return issues.ToDictionary(
                issue => issue.Key,
                issue => IssueDto.Create(issue.Value, _linkGenerator));
        }

		/// <summary>
		/// Get issue
		/// </summary>
		/// <param name="issueKey"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<IssueDto> GetIssueAsync(
			string issueKey,
			CancellationToken cancellationToken = default)
		{
			var issue = await _jiraClient.Issues.GetIssueAsync(issueKey);
            return issue is null 
                ? default
                : IssueDto.Create(issue, _linkGenerator);
		}

		/// <summary>
		/// Get issues
		/// </summary>
		/// <param name="issueSearchOptions"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<IEnumerable<IssueDto>> GetIssuesAsync(
            IssueSearchOptions issueSearchOptions,
            CancellationToken cancellationToken = default)
        {
            var issues = await _jiraClient.Issues.GetIssuesFromJqlAsync(issueSearchOptions, cancellationToken);
            _logger.LogInformation("GetIssuesAsync successfully. {@entity}", issueSearchOptions);
            return issues.Select(issue => IssueDto.Create(issue, _linkGenerator));
        }

        /// <summary>
        /// Get issue change logs
        /// </summary>
        /// <param name="issueSearchOptions"></param>
        /// <param name="changeLogFilter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<IssueChangeLogDto>> GetIssueChangeLogsAsync(
            IssueSearchOptions issueSearchOptions,
            Func<IssueChangeLog, bool> changeLogFilter = null,
            CancellationToken cancellationToken = default)
        {
            var issues = await GetIssuesAsync(issueSearchOptions, cancellationToken: cancellationToken);
            return await GetIssueChangeLogsAsync(issues, changeLogFilter, cancellationToken);
        }

        /// <summary>
        /// Get issue change logs
        /// </summary>
        /// <param name="issues"></param>
        /// <param name="changeLogFilter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<IssueChangeLogDto>> GetIssueChangeLogsAsync(
            IEnumerable<IssueDto> issues,
            Func<IssueChangeLog, bool> changeLogFilter = null,
            CancellationToken cancellationToken = default)
        {
            var result = new List<IssueChangeLogDto> { };
            foreach (var issue in issues)
            {
                var issueChangeLogs = await _jiraClient.Issues.GetChangeLogsAsync(issue.Key, cancellationToken);
                issueChangeLogs = issueChangeLogs.WhereIfNotNull(changeLogFilter);

                result.AddRange(issueChangeLogs.Select(issueChangeLog =>
                    IssueChangeLogDto.Create(issueChangeLog, issue)));
            }

            return result;
        }

        /// <summary>
        /// Get issue worklogs
        /// </summary>
        /// <param name="issueSearchOptions"></param>
        /// <param name="worklogFilter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<IssueWorklogDto>> GetIssueWorklogsAsync(
            IssueSearchOptions issueSearchOptions,
            Func<Worklog, bool> worklogFilter = null,
            CancellationToken cancellationToken = default)
        {
            var issues = await GetIssuesAsync(issueSearchOptions, cancellationToken: cancellationToken);
            return await GetIssueWorklogsAsync(issues, worklogFilter, cancellationToken);
        }

        /// <summary>
        /// Get issue worklogs
        /// </summary>
        /// <param name="issues"></param>
        /// <param name="worklogFilter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<IssueWorklogDto>> GetIssueWorklogsAsync(
            IEnumerable<IssueDto> issues,
            Func<Worklog, bool> worklogFilter = null,
            CancellationToken cancellationToken = default)
        {
            var result = new ConcurrentBag<IssueWorklogDto> { };
            await Parallel.ForEachAsync(issues, DefaultParallelOptions, async (issue, cancellationToken) =>
            {
                var issueWorklogs = await _jiraClient.Issues.GetWorklogsAsync(issue.Key, cancellationToken);
                issueWorklogs = issueWorklogs.WhereIfNotNull(worklogFilter);
                foreach (var issueWorklog in issueWorklogs)
                {
                    result.Add(IssueWorklogDto.Create(issueWorklog, issue));
                }
            });

            return result;
        }

        /// <summary>
        /// Get issue change log items
        /// </summary>
        /// <param name="issues"></param>
        /// <param name="changeLogFilter"></param>
        /// <param name="changeLogItemFilter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<IssueChangeLogItemDto>> GetIssueChangeLogItemsAsync(
            IEnumerable<IssueDto> issues,
            Func<IssueChangeLog, bool> changeLogFilter = null,
            Func<IssueChangeLogItem, bool> changeLogItemFilter = null,
            CancellationToken cancellationToken = default)
        {
            var result = new ConcurrentBag<IssueChangeLogItemDto> { };
            await Parallel.ForEachAsync(issues, DefaultParallelOptions, async (issue, cancellationToken) =>
            {
                var issueChangeLogs = await _jiraClient.Issues.GetChangeLogsAsync(issue.Key, cancellationToken);
                issueChangeLogs = issueChangeLogs.WhereIfNotNull(changeLogFilter);

                foreach (var issueChangeLog in issueChangeLogs)
                {
                    var issueChangeLogItems = issueChangeLog.Items;
                    issueChangeLogItems = issueChangeLogItems.WhereIfNotNull(changeLogItemFilter);
                    foreach (var issueChangeLogItem in issueChangeLogItems)
                    {
                        result.Add(new IssueChangeLogItemDto
                        {
                            FromId = issueChangeLogItem.FromId,
                            ToId = issueChangeLogItem.ToId,
                            FromValue = issueChangeLogItem.FromValue,
                            ToValue = issueChangeLogItem.ToValue,
                            ChangeLog = IssueChangeLogDto.Create(issueChangeLog, issue),
                            Author = issueChangeLog.Author.Username
                        });
                    }
                }
            });

            return result;
        }

        /// <summary>
        /// Get current user
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<UserDto> GetCurrentUserAsync(
            CancellationToken cancellationToken = default)
        {
            var myself = await _jiraClient.Users.GetMyselfAsync(cancellationToken);
            var userData = _jiraClient.RestClient.DownloadData(myself.Self);
            var timeZoneId = GetJsonParameterValue(userData, "timeZone");
            var avatarUrl = myself.AvatarUrls.Large;
            var avatar = _jiraClient.RestClient.DownloadData(avatarUrl);
            string img64 = Convert.ToBase64String(avatar);
            string urlData = string.Format("data:image/jpg;base64, {0}", img64);

            return new UserDto
            {
                Username = myself.Username,
                TimeZoneId = timeZoneId,
                Avatar = urlData
            };
        }

        /// <summary>
        /// Add worklog
        /// </summary>
        /// <param name="worklogDto"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>        
        public async Task AddWorklogAsync(
            AddedWorklogDto worklogDto,
            CancellationToken cancellationToken = default)
        {
            var minutesLag = worklogDto.ElapsedTime.Seconds >= 30 ? 1 : 0;
            var worklog = new Worklog(
                $"{worklogDto.ElapsedTime.Hours}h {worklogDto.ElapsedTime.Minutes + minutesLag}m",
                worklogDto.StartedAt,
                worklogDto.Comment);
            await _jiraClient.Issues.AddWorklogAsync(worklogDto.IssueKey, worklog, token: cancellationToken);
            _logger.LogInformation("Worklog added successfully. {@entity}", worklogDto);
        }

        /// <summary>
        /// Login by basic auth
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<LoginResponse> LoginAsync(
            BasicLoginRequest request,
            CancellationToken cancellationToken = default)
        {
            var jiraClient = Atlassian.Jira.Jira.CreateRestClient(_config.Url, request.Username, request.Password);
            return await LoginAsync(jiraClient, cancellationToken);
        }

        /// <summary>
        /// Login by personal access token
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<LoginResponse> LoginAsync(
            BearerLoginRequest request,
            CancellationToken cancellationToken = default)
        {
            var jiraClient = CreateBearerRestClient(_config.Url, request.Token);
            return await LoginAsync(jiraClient, cancellationToken);
        }

        private async Task<LoginResponse> LoginAsync(
            Atlassian.Jira.Jira jiraClient,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var myself = await jiraClient.Users.GetMyselfAsync(token: cancellationToken);
                _logger.LogInformation("Login successfully");
                return new LoginResponse(true) { Username = myself.Username };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                throw;
            }
        }

        public async Task<string> GetCurrentUserAvatarAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var myself = await _jiraClient.Users.GetMyselfAsync(cancellationToken);
                var avatarUrl = myself.AvatarUrls.Large;
                var avatar = _jiraClient.RestClient.DownloadData(avatarUrl);
                string img64 = Convert.ToBase64String(avatar);
                string urlData = string.Format("data:image/jpg;base64, {0}", img64);
                return urlData;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<IEnumerable<IssueStatusDto>> GetIssueStatusesAsync(
            CancellationToken cancellationToken = default)
        {
            var issueStatuses = await _jiraClient.Statuses.GetStatusesAsync(cancellationToken);
            return issueStatuses.Select(issueStatus => IssueStatusDto.Create(issueStatus));
        }

        private static string GetJsonParameterValue(byte[] jsonObject, string parameter)
        {
            var json = System.Text.Encoding.UTF8.GetString(jsonObject);
            var jsonNode = JsonNode.Parse(json);
            return (string)jsonNode[parameter];
        }

        /// <summary>
        /// Get issues comments
        /// </summary>
        /// <param name="issues"></param>
        /// <param name="filter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<IEnumerable<IssueCommentDto>> GetIssueCommentsAsync(
            IEnumerable<IssueDto> issues,
            Func<Comment, bool> filter = null,
            CancellationToken cancellationToken = default)
        {
            var result = new ConcurrentBag<IssueCommentDto> { };
            await Parallel.ForEachAsync(issues, DefaultParallelOptions, async (issue, cancellationToken) =>
            {
                var options = new CommentQueryOptions();
                var comments = await _jiraClient.Issues.GetCommentsAsync(issue.Key, options, cancellationToken);
                comments = comments.WhereIfNotNull(filter);
                foreach (var comment in comments)
                {
                    result.Add(IssueCommentDto.Create(comment, issue));
                }
            });

            return result;
        }

        public async Task<HttpStatusCode> PingAsync(CancellationToken cancellationToken = default)
        {
            HttpClient httpClient = new();
            var content = await httpClient.GetAsync(_config.Url, cancellationToken);
            return content.StatusCode;
        }

        /// <summary>
        /// /rest/dev-status/latest/issue/detail?issueId=<JIRA-IDENTIFIER>&applicationType=<APPLICATION-TYPE>&dataType=<DATA_TYPE>
        /// </summary>
        /// <param name="jiraIdentifier"></param>
        /// <param name="applicationType"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public async Task<DevStatusDetailDto> GetIssueDevStatusDetailAsync(string jiraIdentifier, string applicationType = "github",
            string dataType = "pullrequest", CancellationToken cancellationToken = default)
        {
            return await _jiraClient.RestClient
                .ExecuteRequestAsync<DevStatusDetailDto>(
                method: RestSharp.Method.GET,
                resource: $"/rest/dev-status/latest/issue/detail?issueId={jiraIdentifier}&applicationType={applicationType}&dataType={dataType}",
                token: cancellationToken);
        }

        /// <summary>
        /// Get worklogs via the Tempo Timesheets REST API, filtered by date on the
        /// server side. Unlike the per-issue Jira worklog endpoint, this does not pull
        /// every worklog of an issue into memory. See issue #245.
        /// </summary>
        /// <param name="dateFrom">Inclusive lower bound of the worklog date.</param>
        /// <param name="dateTo">Inclusive upper bound of the worklog date.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TempoWorklogDto>> GetTempoWorklogsAsync(
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default)
        {
            // Fixed Tempo Timesheets Server/DC v3 endpoint. Not configurable: the
            // TempoWorklogDto contract is tied to this version's response shape.
            const string tempoWorklogsPath = "/rest/tempo-timesheets/3/worklogs";
            var resource = $"{tempoWorklogsPath}" +
                $"?dateFrom={dateFrom:yyyy-MM-dd}&dateTo={dateTo:yyyy-MM-dd}";

            var worklogs = await _jiraClient.RestClient
                .ExecuteRequestAsync<TempoWorklogDto[]>(
                    method: RestSharp.Method.GET,
                    resource: resource,
                    token: cancellationToken);

            return worklogs ?? Array.Empty<TempoWorklogDto>();
        }

        /// <summary>
        /// Get the Jira server's current UTC offset (from /rest/api/2/serverInfo), cached
        /// process-wide. Needed to interpret naive Tempo worklog timestamps, which are in
        /// the Jira server timezone. See issue #245.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TimeSpan> GetServerUtcOffsetAsync(
            CancellationToken cancellationToken = default)
        {
            if (_cachedServerUtcOffset.HasValue)
            {
                return _cachedServerUtcOffset.Value;
            }

            await ServerUtcOffsetLock.WaitAsync(cancellationToken);
            try
            {
                if (_cachedServerUtcOffset.HasValue)
                {
                    return _cachedServerUtcOffset.Value;
                }

                // Download raw JSON and parse with System.Text.Json. The Newtonsoft
                // serializer behind ExecuteRequestAsync auto-converts the "serverTime"
                // datetime string to the app host's local timezone, corrupting the offset
                // (e.g. returns +4 on a +4 host for a +3 server). System.Text.Json keeps
                // the string verbatim. See issue #245.
                var serverInfoUrl = _config.Url.AppendUrl("rest", "api", "2", "serverInfo");
                var serverInfoData = _jiraClient.RestClient.DownloadData(serverInfoUrl);
                var serverTime = GetJsonParameterValue(serverInfoData, "serverTime");

                var offset = DateTimeOffset.TryParse(
                    serverTime,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedServerTime)
                    ? parsedServerTime.Offset
                    : TimeZoneInfo.Local.BaseUtcOffset;

                _cachedServerUtcOffset = offset;
                return offset;
            }
            finally
            {
                ServerUtcOffsetLock.Release();
            }
        }

        private static Atlassian.Jira.Jira CreateBearerRestClient(string url, string bearerToken)
        {
            var authenticator = new JwtAuthenticator(bearerToken);
            var jiraRestClient = new JiraRestClient(url);
            jiraRestClient.RestSharpClient.Authenticator = authenticator;
            var jiraClient = Atlassian.Jira.Jira.CreateRestClient(jiraRestClient);
            return jiraClient;
        }
    }
}
