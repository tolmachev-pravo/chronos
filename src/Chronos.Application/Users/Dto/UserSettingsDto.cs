using System;

namespace Chronos.Application.Users.Dto
{
    /// <summary>
    /// Working day of a user: the frame every estimated worklog is fitted into.
    /// Stored per user in <see cref="Domain.Entities.Users.UserSettings"/> (issue #241).
    /// </summary>
    public record UserSettingsDto(
        TimeSpan WorkingStartTime,
        TimeSpan WorkingEndTime,
        TimeSpan LunchTime)
    {
        /// <summary>
        /// Defaults for a user without stored settings — the values the worklog filter
        /// used to start with.
        /// </summary>
        public static UserSettingsDto Default => new(
            WorkingStartTime: TimeSpan.FromHours(10),
            WorkingEndTime: TimeSpan.FromHours(19),
            LunchTime: TimeSpan.FromHours(1));
    }
}
