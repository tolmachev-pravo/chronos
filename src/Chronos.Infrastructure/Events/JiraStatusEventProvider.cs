using Atlassian.Jira;
using Chronos.Application.Events;
using Chronos.Application.Extensions.Jira;
using Chronos.Application.Extensions.Jira.Dto;
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
    /// Events derived from the status history of the issues a user is responsible for.
    /// Assignee and tester events differ only in the JQL field naming the user and in
    /// the status whose intervals are collected. Moved out of JiraWorklogDataSource,
    /// where the same code lived twice. See issue #299.
    /// </summary>
    public abstract class JiraStatusEventProvider : IEventProvider
    {
        private readonly IJiraService _jiraService;
        private readonly IJiraQueryFactory _queryFactory;
        private readonly IStorage<string, UserProfile> _userProfileStorage;
        private readonly ITimeProvider _timeProvider;
        private readonly IJiraExtensionProvider _extensionProvider;

        private EventQuery _query;
        private UserProfile _userProfile;

        protected JiraStatusEventProvider(
            IJiraService jiraService,
            IJiraQueryFactory queryFactory,
            IStorage<string, UserProfile> userProfileStorage,
            ITimeProvider timeProvider,
            IJiraExtensionProvider extensionProvider)
        {
            _jiraService = jiraService;
            _queryFactory = queryFactory;
            _userProfileStorage = userProfileStorage;
            _timeProvider = timeProvider;
            _extensionProvider = extensionProvider;
        }

        public abstract EventSource Source { get; }

        /// <summary>
        /// The JQL field naming the user: "assignee" or "Tester".
        /// </summary>
        protected abstract string UserField { get; }

        /// <summary>
        /// The status whose intervals become events.
        /// </summary>
        protected abstract string StatusName { get; }

        protected abstract bool IsEnabled(JiraExtensionSettingsDto settings);

        public async Task<bool> PrepareAsync(EventQuery query, CancellationToken cancellationToken = default)
        {
            var extension = await _extensionProvider.GetAsync(query.Username, cancellationToken);
            if (!extension.IsEnabled || !IsEnabled(extension.Settings))
            {
                return false;
            }

            _query = query;
            _userProfile = await _userProfileStorage.GetValueAsync(query.Username, cancellationToken);
            return true;
        }

        public async Task<IEnumerable<IEvent>> GetEventsAsync(CancellationToken cancellationToken = default)
        {
            var issueQuery = _queryFactory.Create()
                .Where(UserField, JiraQueryComparisonType.Equal, JiraQueryMacros.CurrentUser)
                .Where("type", JiraQueryComparisonType.NotEqual, "Story")
                .WhereWas("status", StatusName, _query.StartDate, _query.EndDate)
                .OrderBy("updatedDate", JiraQueryOrderType.Desc)
                .ToString();
            var issueSearchOptions = JiraEventSearchOptions.Create(issueQuery);

            var issues = await _jiraService.GetIssuesAsync(issueSearchOptions, cancellationToken);

            var changeLogFilter = new Func<IssueChangeLog, bool>(changeLog =>
                changeLog.Items.Any(item => item.FieldName == JiraConstants.Status.FieldName));

            // Match the status by name, consistent with the JQL above — avoids depending
            // on a hardcoded status id that varies between Jira instances. See issue #258.
            var changeLogItemFilter = new Func<IssueChangeLogItem, bool>(changeLogItem =>
                changeLogItem.FieldName == JiraConstants.Status.FieldName
                && (changeLogItem.ToValue == StatusName
                    || changeLogItem.FromValue == StatusName));

            var issueChangeLogItems = await _jiraService.GetIssueChangeLogItemsAsync(
                issues, changeLogFilter, changeLogItemFilter, cancellationToken);

            var result = new List<UserEvent>();
            foreach (var issue in issues)
            {
                var events = issueChangeLogItems
                    .Where(item => item.ChangeLog.Issue.Key == issue.Key)
                    .OrderBy(item => item.ChangeLog.CreatedDate)
                    .ToList()
                    .ConvertTo(StatusName, _timeProvider, _userProfile.TimeZoneInfo, Source)
                    .Where(userEvent => userEvent.IsBetween(_query.StartDate, _query.EndDate));
                result.AddRange(events);
            }

            return result.Where(item => item.Author == _userProfile.Username);
        }
    }
}
