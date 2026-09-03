using Atlassian.Jira;
using Chronos.Application.Authentication;
using Chronos.Application.Storage;
using Chronos.Application.Time;
using Chronos.Application.Worklogs;
using Chronos.Application.Worklogs.Queries;
using Chronos.Domain.Models.Users;
using Chronos.Domain.Models.Worklogs;
using Chronos.Infrastructure.Jira.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Jira
{
	public class JiraWorklogDataSource : IWorklogDataSource
	{
		private readonly IJiraService _jiraService;
		private readonly IJiraQueryFactory _queryFactory;
		private readonly IIdentityService _identityService;
		private readonly ITimeProvider _timeProvider;
		private readonly IStorage<string, UserProfile> _userProfileStorage;

		public JiraWorklogDataSource(
			IJiraService jiraService,
			IJiraQueryFactory queryFactory,
			IIdentityService identityService,
			ITimeProvider timeProvider,
			IStorage<string, UserProfile> userProfileStorage)
		{
			_jiraService = jiraService;
			_queryFactory = queryFactory;
			_identityService = identityService;
			_timeProvider = timeProvider;
			_userProfileStorage = userProfileStorage;
		}

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
	}
}
