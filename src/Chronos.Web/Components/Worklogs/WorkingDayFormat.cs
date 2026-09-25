using Chronos.Application.Users.Dto;
using System;

namespace Chronos.Web.Components.Worklogs
{
    /// <summary>
    /// How the working day from the profile reads on the worklog page. It stays in view
    /// next to the period because it is where the numbers in the result come from
    /// (issue #241).
    /// </summary>
    public static class WorkingDayFormat
    {
        /// <summary>«09:00–19:00»</summary>
        public static string Hours(UserSettingsDto settings) =>
            $"{Time(settings.WorkingStartTime)}–{Time(settings.WorkingEndTime)}";

        /// <summary>«обед 2 ч · норма 8 ч»</summary>
        public static string LunchAndNorm(UserSettingsDto settings) =>
            $"обед {Duration(settings.LunchTime)} · норма {Duration(Norm(settings))}";

        /// <summary>
        /// The hours a working day is expected to hold. Nobody works out «09:00–19:00
        /// minus 2 hours» in their head, and this is the number the day is compared to.
        /// </summary>
        public static TimeSpan Norm(UserSettingsDto settings) =>
            settings.WorkingEndTime - settings.WorkingStartTime - settings.LunchTime;

        public static string Duration(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
                return "нет";
            if (value.Minutes == 0)
                return $"{(int)value.TotalHours} ч";
            if (value.TotalHours < 1)
                return $"{value.Minutes} мин";
            return $"{(int)value.TotalHours} ч {value.Minutes} мин";
        }

        private static string Time(TimeSpan value) => value.ToString(@"hh\:mm");
    }
}
