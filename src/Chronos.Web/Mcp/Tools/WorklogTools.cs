using MediatR;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Chronos.Application.Authentication;
using Chronos.Application.Issues;
using Chronos.Application.Storage;
using Chronos.Application.Users.Queries;
using Chronos.Application.Worklogs.Commands;
using Chronos.Application.Worklogs.Dto;
using Chronos.Application.Worklogs.Queries;
using Chronos.Domain.Models.Users;
using Chronos.Web.Mcp.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Web.Mcp.Tools
{
    /// <summary>
    /// What a client can do with worklogs (issue #298). Every tool is a wrapper over a
    /// scenario the site already runs: the day is assembled, the time is logged and the
    /// working day is read by the same handlers the pages use, so a client and a browser
    /// can never disagree about what a day looks like.
    ///
    /// Whose worklogs these are is decided by the personal access token the request came
    /// with — see <see cref="Chronos.Web.Authentication.PersonalAccessTokenAuthenticationHandler"/>.
    /// </summary>
    [McpServerToolType]
    public class WorklogTools
    {
        /// <summary>
        /// A period is read from Jira day by day, so an open-ended one is a request that
        /// never comes back. Two months is longer than any question worth asking about
        /// one's own week.
        /// </summary>
        private const int MaximumPeriodDays = 62;

        /// <summary>
        /// Nobody works longer than a day in a day. A larger number is a mistake — minutes
        /// confused with seconds, most likely — and it would go straight into Jira.
        /// </summary>
        private const int MaximumWorklogMinutes = 24 * 60;

        private readonly IMediator _mediator;
        private readonly IIssueDataSource _issueDataSource;
        private readonly IIdentityService _identityService;
        private readonly IStorage<string, UserProfile> _userProfileStorage;
        private readonly ILogger<WorklogTools> _logger;

        public WorklogTools(
            IMediator mediator,
            IIssueDataSource issueDataSource,
            IIdentityService identityService,
            IStorage<string, UserProfile> userProfileStorage,
            ILogger<WorklogTools> logger)
        {
            _mediator = mediator;
            _issueDataSource = issueDataSource;
            _identityService = identityService;
            _userProfileStorage = userProfileStorage;
            _logger = logger;
        }

        [McpServerTool(Name = "get_worklog_collection", Title = "Worklogs of a period", ReadOnly = true)]
        [Description(
            "Returns the working days of a period: the time already logged in Jira, the time " +
            "Chronos suggests logging on top of it, and how much of the day the two of them " +
            "cover. This is what answers what the user did last week and what is still not " +
            "logged. Durations are minutes. The blocked list holds time the user spent on " +
            "something carrying no issue key — a meeting, usually: it cannot be logged as it " +
            "is, so ask the user which issue it belongs to and log it with add_worklog.")]
        public async Task<IReadOnlyList<WorkingDayView>> GetWorklogCollection(
            [Description("First day of the period, ISO date, for example 2026-09-01.")]
            DateTime startDate,
            [Description("Last day of the period, inclusive, ISO date.")]
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            if (endDate.Date < startDate.Date)
            {
                throw new McpException("The end of the period is earlier than its start");
            }

            if ((endDate.Date - startDate.Date).TotalDays + 1 > MaximumPeriodDays)
            {
                throw new McpException($"A period longer than {MaximumPeriodDays} days cannot be read at once");
            }

            await EnsureUserProfileAsync(cancellationToken);

            var collection = await _mediator.Send(
                new GetWorklogCollection.Query
                {
                    StartDate = startDate.Date,
                    EndDate = endDate.Date
                }, cancellationToken);

            return WorkingDayMapper.ToViews(collection.WorkingDays);
        }

        [McpServerTool(Name = "get_user_settings", Title = "Working day of the user", ReadOnly = true)]
        [Description(
            "Returns the working day from the user's Chronos profile — when it starts, when " +
            "it ends and how long lunch is. Every suggestion is fitted into this frame.")]
        public async Task<UserSettingsView> GetUserSettings(
            CancellationToken cancellationToken = default)
        {
            var user = await _identityService.GetCurrentUserAsync();
            var settings = await _mediator.Send(new GetUserSettings.Query(user?.Username), cancellationToken);

            return new UserSettingsView(
                Username: user?.Username,
                WorkingStartTime: settings.WorkingStartTime.ToString(@"hh\:mm"),
                WorkingEndTime: settings.WorkingEndTime.ToString(@"hh\:mm"),
                LunchMinutes: (int)settings.LunchTime.TotalMinutes,
                WorkingMinutes: (int)(settings.WorkingEndTime - settings.WorkingStartTime - settings.LunchTime).TotalMinutes);
        }

        [McpServerTool(Name = "get_issue", Title = "Issue by key", ReadOnly = true)]
        [Description(
            "Returns the Jira issue with the given key. Use it to check that a key exists and " +
            "means what the user thinks it means before logging time against it.")]
        public async Task<IssueView> GetIssue(
            [Description("Issue key, for example CH-101.")]
            string issueKey,
            CancellationToken cancellationToken = default)
        {
            var issue = await ResolveIssueAsync(issueKey, cancellationToken);
            return new IssueView(issue.Key, issue.Summary, issue.Link);
        }

        [McpServerTool(
            Name = "add_worklog",
            Title = "Log time in Jira",
            ReadOnly = false,
            Destructive = true,
            Idempotent = false,
            OpenWorld = true)]
        [Description(
            "Logs time against a Jira issue on behalf of the user. The worklog appears in Jira " +
            "at once and Chronos cannot take it back — ask the user to confirm the issue, the " +
            "time and the comment before calling this. Calling it twice logs the time twice.")]
        public async Task<AddedWorklogView> AddWorklog(
            [Description("Issue key to log the time against, for example CH-101.")]
            string issueKey,
            [Description("When the work started, ISO date and time in the user's own time zone.")]
            DateTime startedAt,
            [Description("How long the work took, in minutes.")]
            int minutes,
            [Description("Worklog comment — what was done. Shown in Jira next to the time.")]
            string comment = null,
            CancellationToken cancellationToken = default)
        {
            if (minutes <= 0)
            {
                throw new McpException("A worklog needs a positive number of minutes");
            }

            if (minutes > MaximumWorklogMinutes)
            {
                throw new McpException($"A single worklog cannot be longer than {MaximumWorklogMinutes} minutes");
            }

            await EnsureUserProfileAsync(cancellationToken);

            var issue = await ResolveIssueAsync(issueKey, cancellationToken);
            var worklog = new AddedWorklogDto
            {
                IssueKey = issue.Key,
                Issue = issue,
                StartedAt = startedAt,
                ElapsedTime = TimeSpan.FromMinutes(minutes),
                Comment = comment
            };

            var result = await _mediator.Send(new AddWorklog.Command(worklog), cancellationToken);

            // The one line that says a worklog came from a client and not from the site.
            // Nothing else distinguishes them afterwards: Jira records the user, not what
            // they used to log the time.
            _logger.LogInformation(
                "MCP client logged {Minutes} minutes to {IssueKey} at {StartedAt:yyyy-MM-dd HH:mm}",
                minutes,
                issue.Key,
                startedAt);

            return new AddedWorklogView(
                IssueKey: result.Worklog.IssueKey,
                Summary: issue.Summary,
                StartedAt: result.Worklog.StartedAt,
                Minutes: (int)result.Worklog.ElapsedTime.TotalMinutes,
                Comment: result.Worklog.Comment);
        }

        /// <summary>
        /// The user profile — the Jira account behind the worklogs and the time zone their
        /// hours are read in — normally reaches the scenarios from the browser's local
        /// storage. A client request has no browser, and the storage answers null there, so
        /// the profile is asked for from Jira first and kept in the process cache. Without
        /// it a day is assembled for nobody: worklogs are filtered by the profile username
        /// and a logged time is converted by the profile time zone.
        /// </summary>
        private async Task EnsureUserProfileAsync(CancellationToken cancellationToken)
        {
            var user = await _identityService.GetCurrentUserAsync();
            if (user?.Username is null)
            {
                throw new McpException("The request is not authenticated");
            }

            await _userProfileStorage.InitAsync(user.Username, cancellationToken);
        }

        /// <summary>
        /// A worklog carries the issue itself, not only its key, so the key is resolved
        /// before anything is written. An unknown key stops here instead of reaching Jira.
        /// </summary>
        private async Task<Domain.Models.Issues.Issue> ResolveIssueAsync(
            string issueKey,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(issueKey))
            {
                throw new McpException("An issue key is required");
            }

            var issue = await _issueDataSource.GetIssueAsync(issueKey.Trim(), cancellationToken);
            if (issue is null)
            {
                throw new McpException($"Jira knows no issue {issueKey}");
            }

            return issue;
        }
    }
}
