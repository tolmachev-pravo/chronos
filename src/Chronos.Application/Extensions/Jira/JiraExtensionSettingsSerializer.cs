using Chronos.Application.Extensions.Jira.Dto;
using System;
using System.Text.Json;

namespace Chronos.Application.Extensions.Jira
{
    /// <summary>
    /// Converts the Jira extension settings to and from the JSON stored in
    /// <see cref="Domain.Entities.Extensions.UserExtension.Settings"/>. The duration is
    /// stored as a string because System.Text.Json has no built-in TimeSpan support.
    /// </summary>
    public static class JiraExtensionSettingsSerializer
    {
        public static string Serialize(JiraExtensionSettingsDto settings)
            => JsonSerializer.Serialize(new StoredSettings(
                settings.AssigneeEventsEnabled,
                settings.CommentEventsEnabled,
                settings.CommentWorklogTime.ToString(),
                settings.TesterEventsEnabled));

        /// <summary>
        /// Returns <see cref="JiraExtensionSettingsDto.Default"/> when the stored JSON is
        /// missing or unreadable, so a broken record never blocks the worklog collection.
        /// </summary>
        public static JiraExtensionSettingsDto Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
                return JiraExtensionSettingsDto.Default;

            StoredSettings? stored;
            try
            {
                stored = JsonSerializer.Deserialize<StoredSettings>(json);
            }
            catch (JsonException)
            {
                return JiraExtensionSettingsDto.Default;
            }

            if (stored is null)
                return JiraExtensionSettingsDto.Default;

            var commentWorklogTime = TimeSpan.TryParse(stored.CommentWorklogTime, out var parsed)
                ? parsed
                : TimeSpan.Zero;

            return new JiraExtensionSettingsDto(
                stored.AssigneeEventsEnabled,
                stored.CommentEventsEnabled,
                commentWorklogTime,
                stored.TesterEventsEnabled);
        }

        private record StoredSettings(
            bool AssigneeEventsEnabled,
            bool CommentEventsEnabled,
            string CommentWorklogTime,
            bool TesterEventsEnabled);
    }
}
