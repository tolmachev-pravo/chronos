using System;

namespace Pet.Jira.Application.Extensions.Jira.Dto
{
    /// <summary>
    /// Which kinds of Jira events are pulled into the worklog collection.
    /// Stored as JSON in <see cref="Domain.Entities.Extensions.UserExtension.Settings"/>.
    /// </summary>
    public record JiraExtensionSettingsDto(
        bool AssigneeEventsEnabled,
        bool CommentEventsEnabled,
        TimeSpan CommentWorklogTime,
        bool TesterEventsEnabled)
    {
        /// <summary>
        /// Defaults for a user without a stored extension: assignee events only.
        /// </summary>
        public static JiraExtensionSettingsDto Default => new(
            AssigneeEventsEnabled: true,
            CommentEventsEnabled: false,
            CommentWorklogTime: TimeSpan.Zero,
            TesterEventsEnabled: false);
    }
}
