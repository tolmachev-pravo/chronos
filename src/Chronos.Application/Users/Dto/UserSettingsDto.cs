using System;

namespace Chronos.Application.Users.Dto
{
    /// <summary>
    /// Working day of a user: the frame every estimated worklog is fitted into.
    /// Stored per user in <see cref="Domain.Entities.Users.UserSettings"/> (issue #241).
    /// </summary>
    /// <param name="ShortenPreHolidayDays">
    /// Whether a working day before a holiday is an hour shorter. See issue #310.
    /// </param>
    public record UserSettingsDto(
        TimeSpan WorkingStartTime,
        TimeSpan WorkingEndTime,
        TimeSpan LunchTime,
        bool ShortenPreHolidayDays = true)
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
