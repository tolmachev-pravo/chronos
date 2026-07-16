using Microsoft.Extensions.Logging;
using Pet.Jira.Application.Authentication;
using Pet.Jira.Application.Storage;
using Pet.Jira.Application.Worklogs;
using Pet.Jira.Application.Worklogs.Queries;
using Pet.Jira.Domain.Models.Issues;
using Pet.Jira.Domain.Models.Users;
using Pet.Jira.Domain.Models.Worklogs;
using Pet.Jira.Infrastructure.Jira.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Infrastructure.Jira
{
    /// <summary>
    /// Retrieves actual worklogs via the date-filtered Tempo Timesheets REST API
    /// instead of the per-issue Jira worklog endpoint, which loads every worklog of
    /// an issue into memory. See issue #245.
    ///
    /// Only <see cref="GetIssueWorklogsAsync"/> (actual worklogs) uses Tempo; raw
    /// worklogs (derived from changelog/comments/tester) are delegated to the
    /// existing <see cref="JiraWorklogDataSource"/>. If the Tempo call fails, this
    /// source falls back to the Jira implementation so behaviour degrades safely.
    /// </summary>
    public class TempoWorklogDataSource : IWorklogDataSource
    {
        private readonly IJiraService _jiraService;
        private readonly IIdentityService _identityService;
        private readonly IStorage<string, UserProfile> _userProfileStorage;
        private readonly IJiraLinkGenerator _linkGenerator;
        private readonly JiraWorklogDataSource _fallback;
        private readonly ILogger<TempoWorklogDataSource> _logger;

        public TempoWorklogDataSource(
            IJiraService jiraService,
            IIdentityService identityService,
            IStorage<string, UserProfile> userProfileStorage,
            IJiraLinkGenerator linkGenerator,
            JiraWorklogDataSource fallback,
            ILogger<TempoWorklogDataSource> logger)
        {
            _jiraService = jiraService;
            _identityService = identityService;
            _userProfileStorage = userProfileStorage;
            _linkGenerator = linkGenerator;
            _fallback = fallback;
            _logger = logger;
        }

        /// <summary>
        /// Get the current user's actual worklogs for the requested period via Tempo.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<IWorklog>> GetIssueWorklogsAsync(
            GetIssueWorklogs.Query query,
            CancellationToken cancellationToken = default)
        {
            var user = await _identityService.GetCurrentUserAsync();
            var userProfile = await _userProfileStorage.GetValueAsync(user.Key, cancellationToken);

            try
            {
                var serverUtcOffset = await _jiraService.GetServerUtcOffsetAsync(cancellationToken);

                var tempoWorklogs = await _jiraService.GetTempoWorklogsAsync(
                    query.StartDate, query.EndDate, cancellationToken);

                return tempoWorklogs
                    .Where(worklog => IsAuthoredByCurrentUser(worklog, userProfile))
                    .Select(worklog => Map(worklog, userProfile, serverUtcOffset))
                    .Where(worklog => worklog != null
                        && worklog.StartDate >= query.StartDate
                        && worklog.StartDate <= query.EndDate)
                    .ToList();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception,
                    "Tempo worklog retrieval failed; falling back to the Jira worklog source.");
                return await _fallback.GetIssueWorklogsAsync(query, cancellationToken);
            }
        }

        /// <summary>
        /// Raw worklogs (changelog/comment/tester based) are not provided by Tempo —
        /// delegate to the existing Jira implementation.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IEnumerable<IWorklog>> GetRawIssueWorklogsAsync(
            GetRawIssueWorklogs.Query query,
            CancellationToken cancellationToken = default)
            => _fallback.GetRawIssueWorklogsAsync(query, cancellationToken);

        private static bool IsAuthoredByCurrentUser(TempoWorklogDto worklog, UserProfile userProfile)
            => string.Equals(worklog.Author?.Login, userProfile.Username, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Maps a Tempo worklog to the domain model. Tempo returns a naive start time in
        /// the Jira server timezone; it is anchored to <paramref name="serverUtcOffset"/>
        /// and converted to the user's timezone. Returns null when the start date is
        /// missing or unparsable. See issue #245.
        /// </summary>
        private IssueWorklog Map(TempoWorklogDto worklog, UserProfile userProfile, TimeSpan serverUtcOffset)
        {
            var startDate = worklog.GetStartDateTime();
            if (startDate == null)
            {
                return null;
            }

            var userTimeZone = userProfile.TimeZoneInfo;
            var startInstant = new DateTimeOffset(
                DateTime.SpecifyKind(startDate.Value, DateTimeKind.Unspecified), serverUtcOffset);
            var start = startInstant.ToOffset(userTimeZone.GetUtcOffset(startInstant)).DateTime;

            return new IssueWorklog
            {
                StartDate = start,
                CompleteDate = start.AddSeconds(worklog.TimeSpentInSeconds),
                TimeSpent = TimeSpan.FromSeconds(worklog.TimeSpentInSeconds),
                Author = worklog.Author?.Login,
                Issue = new Issue
                {
                    Key = worklog.Issue?.Key,
                    Summary = worklog.Issue?.Summary,
                    Identifier = worklog.Issue?.Id.ToString(),
                    Link = worklog.Issue?.Key != null ? _linkGenerator.Generate(worklog.Issue.Key) : null
                }
            };
        }
    }
}
