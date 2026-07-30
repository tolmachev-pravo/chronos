using Atlassian.Jira;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pet.Jira.Application.Authentication;
using Pet.Jira.Application.Storage;
using Pet.Jira.Application.Time;
using Pet.Jira.Application.Worklogs;
using Pet.Jira.Application.Worklogs.Queries;
using Pet.Jira.Domain.Models.Users;
using Pet.Jira.Domain.Models.Worklogs;
using Pet.Jira.Infrastructure.Jira.Dto;
using Pet.Jira.Infrastructure.Jira.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Infrastructure.Jira
{
	public class JiraWorklogDataSource : IWorklogDataSource
	{
		private readonly IJiraService _jiraService;
		private readonly IJiraQueryFactory _queryFactory;
		private readonly IIdentityService _identityService;
		private readonly ITimeProvider _timeProvider;
		private readonly IStorage<string, UserProfile> _userProfileStorage;
		private readonly IJiraConfiguration _configuration;
		private readonly ILogger<JiraWorklogDataSource> _logger;

		public JiraWorklogDataSource(
			IJiraService jiraService,
			IJiraQueryFactory queryFactory,
			IIdentityService identityService,
			ITimeProvider timeProvider,
			IStorage<string, UserProfile> userProfileStorage,
			IOptions<JiraConfiguration> configuration,
			ILogger<JiraWorklogDataSource> logger)
		{
			_jiraService = jiraService;
			_queryFactory = queryFactory;
			_identityService = identityService;
			_timeProvider = timeProvider;
			_userProfileStorage = userProfileStorage;
			_configuration = configuration.Value;
			_logger = logger;
		}

		/// <summary>
		/// Search options for the Jira-event queries. The events only need the issue
		/// summary for display; the heavy work is the per-issue changelog/comment fetch
		/// that follows. Restricting the search to the summary field keeps the initial
		/// JQL response small instead of pulling every navigable field. See issue #258.
		/// </summary>
		private static IssueSearchOptions CreateEventSearchOptions(string jql) =>
			new(jql)
			{
				MaxIssuesPerRequest = JiraConstants.DefaultMaxIssuesPerRequest,
				FetchBasicFields = false,
				AdditionalFields = { "summary" }
			};

		public async Task<IEnumerable<IWorklog>> GetIssueWorklogsAsync(
			GetIssueWorklogs.Query query,
			CancellationToken cancellationToken = default)
		{
			var issueQuery = _queryFactory.Create()
				.Where("worklogDate", JiraQueryComparisonType.GreaterOrEqual, query.StartDate)
				.Where("worklogDate", JiraQueryComparisonType.LessOrEqual, query.EndDate)
				.Where("worklogAuthor", JiraQueryComparisonType.Equal, JiraQueryMacros.CurrentUser)
				.OrderBy("updatedDate", JiraQueryOrderType.Desc)
				.ToString();
			var issueSearchOptions = new IssueSearchOptions(issueQuery)
			{
				MaxIssuesPerRequest = JiraConstants.DefaultMaxIssuesPerRequest
			};

			var user = await _identityService.GetCurrentUserAsync();
			var userProfile = await _userProfileStorage.GetValueAsync(user.Key, cancellationToken);

			var worklogFilter = new Func<Worklog, bool>(worklog =>
				worklog.Author == userProfile.Username
				&& worklog.StartDate >= query.StartDate
				&& worklog.StartDate <= query.EndDate);

			var issueWorklogs = await _jiraService.GetIssueWorklogsAsync(issueSearchOptions, worklogFilter, cancellationToken);

			return issueWorklogs.Select(issueWorklog => issueWorklog.Adapt(_timeProvider, userProfile.TimeZoneInfo));
		}

		/// <summary>
		/// Get estimated worklogs from the "In Progress" status changes of issues
		/// assigned to the current user. See issue #258.
		/// </summary>
		public async Task<IEnumerable<IWorklog>> GetAssigneeRawIssueWorklogsAsync(
			GetAssigneeJiraEvents.Query query,
			CancellationToken cancellationToken = default)
		{
			var issueQuery = _queryFactory.Create()
				.Where("assignee", JiraQueryComparisonType.Equal, JiraQueryMacros.CurrentUser)
				.Where("type", JiraQueryComparisonType.NotEqual, "Story")
				.WhereWas("status", JiraConstants.Status.InProgress.Name, query.StartDate, query.EndDate)
				.OrderBy("updatedDate", JiraQueryOrderType.Desc)
				.ToString();
			var issueSearchOptions = CreateEventSearchOptions(issueQuery);

			var issues = await _jiraService.GetIssuesAsync(issueSearchOptions, cancellationToken);

			var user = await _identityService.GetCurrentUserAsync();
			var userProfile = await _userProfileStorage.GetValueAsync(user.Key, cancellationToken);

			var changeLogFilter = new Func<IssueChangeLog, bool>(changeLog =>
				changeLog.Items.Any(item => item.FieldName == JiraConstants.Status.FieldName));

			// Match the status by name, consistent with the JQL above — avoids depending
			// on a hardcoded status id that varies between Jira instances. See issue #258.
			var changeLogItemFilter = new Func<IssueChangeLogItem, bool>(changeLogItem =>
				changeLogItem.FieldName == JiraConstants.Status.FieldName
				&& (changeLogItem.ToValue == JiraConstants.Status.InProgress.Name
					|| changeLogItem.FromValue == JiraConstants.Status.InProgress.Name));

			var issueChangeLogItems = await _jiraService.GetIssueChangeLogItemsAsync(issues, changeLogFilter, changeLogItemFilter, cancellationToken);

			var result = new List<RawIssueWorklog> { };
			foreach (var issue in issues)
			{
				var rawIssueWorklogs = issueChangeLogItems
					.Where(item => item.ChangeLog.Issue.Key == issue.Key)
					.OrderBy(item => item.ChangeLog.CreatedDate)
					.ToList()
					.ConvertTo<RawIssueWorklog>(JiraConstants.Status.InProgress.Name, _timeProvider, userProfile.TimeZoneInfo, WorklogSource.Assignee)
					.Where(issueWorklog => issueWorklog.IsBetween(query.StartDate, query.EndDate));
				result.AddRange(rawIssueWorklogs);
			}

			return result.Where(item => item.Author == userProfile.Username);
		}

		/// <summary>
		/// Get estimated worklogs from the current user's comments on issues they are
		/// not assigned to. See issue #258.
		/// </summary>
		public async Task<IEnumerable<IWorklog>> GetCommentRawIssueWorklogsAsync(
			GetCommentJiraEvents.Query query,
			CancellationToken cancellationToken = default)
		{
			var issues = await GetCommentedIssuesAsync(query, cancellationToken);

			var user = await _identityService.GetCurrentUserAsync();
			var userProfile = await _userProfileStorage.GetValueAsync(user.Key, cancellationToken);

			var filter = new Func<Comment, bool>(comment =>
				comment.Author == userProfile.Username
				&& comment.CreatedDate.Value >= query.StartDate
				&& comment.CreatedDate.Value <= query.EndDate);

			var comments = await _jiraService.GetIssueCommentsAsync(issues, filter, cancellationToken);

			var result = new List<RawIssueWorklog> { };
			foreach (var issue in issues)
			{
				var rawIssueWorklogs = comments
					.Where(item => item.Issue.Key == issue.Key)
					.OrderBy(item => item.CreatedDate)
					.ToList()
					.ConvertTo<RawIssueWorklog>(_timeProvider, userProfile.TimeZoneInfo, WorklogSource.Comment, query.CommentWorklogTime)
					.Where(issueWorklog => issueWorklog.IsBetween(query.StartDate, query.EndDate));
				result.AddRange(rawIssueWorklogs);
			}

			return result.Where(item => item.Author == userProfile.Username);
		}

		/// <summary>
		/// Find the issues the current user commented on during the requested period.
		/// The ScriptRunner "commented" issue function filters by comment author and
		/// period on the Jira side; without the addon installed the search fails, so the
		/// plain query is used instead. See issue #259.
		/// </summary>
		private async Task<IEnumerable<IssueDto>> GetCommentedIssuesAsync(
			GetCommentJiraEvents.Query query,
			CancellationToken cancellationToken)
		{
			if (_configuration.ScriptRunner.Enabled)
			{
				try
				{
					return await _jiraService.GetIssuesAsync(
						CreateEventSearchOptions(CreateCommentedIssueQuery(query)),
						cancellationToken);
				}
				catch (Exception exception)
				{
					_logger.LogWarning(exception,
						"The ScriptRunner commented search failed; falling back to the plain comment query.");
				}
			}

			return await _jiraService.GetIssuesAsync(
				CreateEventSearchOptions(CreateUpdatedIssueQuery(query)),
				cancellationToken);
		}

		/// <summary>
		/// The "commented" bounds are exclusive midnights, so the upper one is shifted a
		/// day forward to keep the last day of the period; the exact interval is still
		/// applied to the comments themselves further down. Watching is not required —
		/// authoring the comment is what the function already matches on. See issue #259.
		/// </summary>
		private string CreateCommentedIssueQuery(GetCommentJiraEvents.Query query) =>
			_queryFactory.Create()
				.WhereCommented(JiraQueryMacros.CurrentUser, query.StartDate, query.EndDate.AddDays(1))
				.Where("assignee", JiraQueryComparisonType.NotEqual, JiraQueryMacros.CurrentUser)
				.OrderBy("updatedDate", JiraQueryOrderType.Desc)
				.ToString();

		/// <summary>
		/// The pre-ScriptRunner query: plain JQL cannot filter by comment author, so every
		/// issue updated since the start of the period has to be scanned. The watcher
		/// condition is what keeps that scan bounded here, so it stays. See issue #259.
		/// </summary>
		private string CreateUpdatedIssueQuery(GetCommentJiraEvents.Query query) =>
			_queryFactory.Create()
				.Where("updatedDate", JiraQueryComparisonType.GreaterOrEqual, query.StartDate)
				.Where("watcher", JiraQueryComparisonType.Equal, JiraQueryMacros.CurrentUser)
				.Where("assignee", JiraQueryComparisonType.NotEqual, JiraQueryMacros.CurrentUser)
				.OrderBy("updatedDate", JiraQueryOrderType.Desc)
				.ToString();

		/// <summary>
		/// Get estimated worklogs from the "In Testing" status changes of issues where
		/// the current user is the tester. See issue #258.
		/// </summary>
		public async Task<IEnumerable<IWorklog>> GetTesterRawIssueWorklogsAsync(
			GetTesterJiraEvents.Query query,
			CancellationToken cancellationToken = default)
		{
			var issueQuery = _queryFactory.Create()
				.Where("Tester", JiraQueryComparisonType.Equal, JiraQueryMacros.CurrentUser)
				.Where("type", JiraQueryComparisonType.NotEqual, "Story")
				.WhereWas("status", JiraConstants.Status.InTesting.Name, query.StartDate, query.EndDate)
				.OrderBy("updatedDate", JiraQueryOrderType.Desc)
				.ToString();
			var issueSearchOptions = CreateEventSearchOptions(issueQuery);

			var issues = await _jiraService.GetIssuesAsync(issueSearchOptions, cancellationToken);

			var user = await _identityService.GetCurrentUserAsync();
			var userProfile = await _userProfileStorage.GetValueAsync(user.Key, cancellationToken);

			var changeLogFilter = new Func<IssueChangeLog, bool>(changeLog =>
				changeLog.Items.Any(item => item.FieldName == JiraConstants.Status.FieldName));

			// Match the "In Testing" status by name in both the filter and ConvertTo, so
			// the interval detection is driven by the same status the changelog items
			// were filtered on — no hardcoded status id. See issue #258.
			var changeLogItemFilter = new Func<IssueChangeLogItem, bool>(changeLogItem =>
				changeLogItem.FieldName == JiraConstants.Status.FieldName
				&& (changeLogItem.ToValue == JiraConstants.Status.InTesting.Name
					|| changeLogItem.FromValue == JiraConstants.Status.InTesting.Name));

			var issueChangeLogItems = await _jiraService.GetIssueChangeLogItemsAsync(issues, changeLogFilter, changeLogItemFilter, cancellationToken);

			var result = new List<RawIssueWorklog> { };
			foreach (var issue in issues)
			{
				var rawIssueWorklogs = issueChangeLogItems
					.Where(item => item.ChangeLog.Issue.Key == issue.Key)
					.OrderBy(item => item.ChangeLog.CreatedDate)
					.ToList()
					.ConvertTo<RawIssueWorklog>(JiraConstants.Status.InTesting.Name, _timeProvider, userProfile.TimeZoneInfo, WorklogSource.Tester)
					.Where(issueWorklog => issueWorklog.IsBetween(query.StartDate, query.EndDate));
				result.AddRange(rawIssueWorklogs);
			}

			return result.Where(item => item.Author == userProfile.Username);
		}
	}
}
