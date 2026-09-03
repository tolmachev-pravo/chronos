using Atlassian.Jira;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Chronos.Application.Events;
using Chronos.Application.Extensions.Jira;
using Chronos.Application.Storage;
using Chronos.Application.Time;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Users;
using Chronos.Infrastructure.Jira;
using Chronos.Infrastructure.Jira.Dto;
using Chronos.Infrastructure.Jira.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Events
{
    /// <summary>
    /// Events from the comments the user left on issues they are not assigned to. Each
    /// comment stands for a fixed slice of time, taken from the extension settings.
    /// Moved out of JiraWorklogDataSource. See issue #299.
    /// </summary>
    public class JiraCommentEventProvider : IEventProvider
    {
        private readonly IJiraService _jiraService;
        private readonly IJiraQueryFactory _queryFactory;
        private readonly IStorage<string, UserProfile> _userProfileStorage;
        private readonly ITimeProvider _timeProvider;
        private readonly IJiraExtensionProvider _extensionProvider;
        private readonly IJiraConfiguration _configuration;
        private readonly ILogger<JiraCommentEventProvider> _logger;

        private EventQuery _query;
        private UserProfile _userProfile;
        private TimeSpan _commentWorklogTime;

        public JiraCommentEventProvider(
            IJiraService jiraService,
            IJiraQueryFactory queryFactory,
            IStorage<string, UserProfile> userProfileStorage,
            ITimeProvider timeProvider,
            IJiraExtensionProvider extensionProvider,
            IOptions<JiraConfiguration> configuration,
            ILogger<JiraCommentEventProvider> logger)
        {
            _jiraService = jiraService;
            _queryFactory = queryFactory;
            _userProfileStorage = userProfileStorage;
            _timeProvider = timeProvider;
            _extensionProvider = extensionProvider;
            _configuration = configuration.Value;
            _logger = logger;
        }

        public EventSource Source => EventSource.Comment;

        public async Task<bool> PrepareAsync(EventQuery query, CancellationToken cancellationToken = default)
        {
            var extension = await _extensionProvider.GetAsync(query.Username, cancellationToken);
            if (!extension.IsEnabled || !extension.Settings.CommentEventsEnabled)
            {
                return false;
            }

            _query = query;
            _commentWorklogTime = extension.Settings.CommentWorklogTime;
            _userProfile = await _userProfileStorage.GetValueAsync(query.Username, cancellationToken);
            return true;
        }

        public async Task<IEnumerable<IEvent>> GetEventsAsync(CancellationToken cancellationToken = default)
        {
            var issues = await GetCommentedIssuesAsync(cancellationToken);

            var filter = new Func<Comment, bool>(comment =>
                comment.Author == _userProfile.Username
                && comment.CreatedDate.Value >= _query.StartDate
                && comment.CreatedDate.Value <= _query.EndDate);

            var comments = await _jiraService.GetIssueCommentsAsync(issues, filter, cancellationToken);

            var result = new List<UserEvent>();
            foreach (var issue in issues)
            {
                var events = comments
                    .Where(item => item.Issue.Key == issue.Key)
                    .OrderBy(item => item.CreatedDate)
                    .ToList()
                    .ConvertTo(_timeProvider, _userProfile.TimeZoneInfo, Source, _commentWorklogTime)
                    .Where(userEvent => userEvent.IsBetween(_query.StartDate, _query.EndDate));
                result.AddRange(events);
            }

            return result.Where(item => item.Author == _userProfile.Username);
        }

        /// <summary>
        /// Find the issues the current user commented on during the requested period.
        /// The ScriptRunner "commented" issue function filters by comment author and
        /// period on the Jira side; without the addon installed the search fails, so the
        /// plain query is used instead. See issue #259.
        /// </summary>
        private async Task<IEnumerable<IssueDto>> GetCommentedIssuesAsync(CancellationToken cancellationToken)
        {
            if (_configuration.ScriptRunner.Enabled)
            {
                try
                {
                    return await _jiraService.GetIssuesAsync(
                        JiraEventSearchOptions.Create(CreateCommentedIssueQuery()),
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception,
                        "The ScriptRunner commented search failed; falling back to the plain comment query.");
                }
            }

            return await _jiraService.GetIssuesAsync(
                JiraEventSearchOptions.Create(CreateUpdatedIssueQuery()),
                cancellationToken);
        }

        /// <summary>
        /// The "commented" bounds are exclusive midnights, so the upper one is shifted a
        /// day forward to keep the last day of the period; the exact interval is still
        /// applied to the comments themselves further down. Watching is not required —
        /// authoring the comment is what the function already matches on. See issue #259.
        /// </summary>
        private string CreateCommentedIssueQuery() =>
            _queryFactory.Create()
                .WhereCommented(JiraQueryMacros.CurrentUser, _query.StartDate, _query.EndDate.AddDays(1))
                .Where("assignee", JiraQueryComparisonType.NotEqual, JiraQueryMacros.CurrentUser)
                .OrderBy("updatedDate", JiraQueryOrderType.Desc)
                .ToString();

        /// <summary>
        /// The pre-ScriptRunner query: plain JQL cannot filter by comment author, so every
        /// issue updated since the start of the period has to be scanned. The watcher
        /// condition is what keeps that scan bounded here, so it stays. See issue #259.
        /// </summary>
        private string CreateUpdatedIssueQuery() =>
            _queryFactory.Create()
                .Where("updatedDate", JiraQueryComparisonType.GreaterOrEqual, _query.StartDate)
                .Where("watcher", JiraQueryComparisonType.Equal, JiraQueryMacros.CurrentUser)
                .Where("assignee", JiraQueryComparisonType.NotEqual, JiraQueryMacros.CurrentUser)
                .OrderBy("updatedDate", JiraQueryOrderType.Desc)
                .ToString();
    }
}
