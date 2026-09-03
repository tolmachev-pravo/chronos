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
    /// Events from the "In Testing" status changes of the issues where the user is the
    /// tester.
    /// </summary>
    public class JiraTesterEventProvider : JiraStatusEventProvider
    {
        public JiraTesterEventProvider(
            IJiraService jiraService,
            IJiraQueryFactory queryFactory,
            IStorage<string, UserProfile> userProfileStorage,
            ITimeProvider timeProvider,
            IJiraExtensionProvider extensionProvider)
            : base(jiraService, queryFactory, userProfileStorage, timeProvider, extensionProvider)
        {
        }

        public override EventSource Source => EventSource.Tester;

        protected override string UserField => "Tester";

        protected override string StatusName => JiraConstants.Status.InTesting.Name;

        protected override bool IsEnabled(JiraExtensionSettingsDto settings) => settings.TesterEventsEnabled;
    }
}
