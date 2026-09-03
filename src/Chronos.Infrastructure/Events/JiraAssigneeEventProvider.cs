using Chronos.Application.Extensions.Jira;
using Chronos.Application.Extensions.Jira.Dto;
using Chronos.Application.Storage;
using Chronos.Application.Time;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Users;
using Chronos.Infrastructure.Jira;
using Chronos.Infrastructure.Jira.Query;

namespace Chronos.Infrastructure.Events
{
    /// <summary>
    /// Events from the "In Progress" status changes of the issues assigned to the user.
    /// </summary>
    public class JiraAssigneeEventProvider : JiraStatusEventProvider
    {
        public JiraAssigneeEventProvider(
            IJiraService jiraService,
            IJiraQueryFactory queryFactory,
            IStorage<string, UserProfile> userProfileStorage,
            ITimeProvider timeProvider,
            IJiraExtensionProvider extensionProvider)
            : base(jiraService, queryFactory, userProfileStorage, timeProvider, extensionProvider)
        {
        }

        public override EventSource Source => EventSource.Assignee;

        protected override string UserField => "assignee";

        protected override string StatusName => JiraConstants.Status.InProgress.Name;

        protected override bool IsEnabled(JiraExtensionSettingsDto settings) => settings.AssigneeEventsEnabled;
    }
}
