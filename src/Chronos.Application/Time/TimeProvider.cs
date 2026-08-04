using System;

namespace Chronos.Application.Time
{
    public class TimeProvider : ITimeProvider
    {
        private static TimeZoneInfo ServerTimezone => TimeZoneInfo.Local;

        public DateTime? ConvertToUserTimezone(DateTime? dateTime, TimeZoneInfo userTimeZone)
        {
            return dateTime == null ? null : ConvertToUserTimezone(dateTime.Value, userTimeZone);
        }

        public DateTime ConvertToUserTimezone(DateTime dateTime, TimeZoneInfo userTimeZone)
        {
            return TimeZoneInfo.ConvertTime(dateTime, ServerTimezone, userTimeZone);
        }

        public DateTime? ConvertToServerTimezone(DateTime? dateTime, TimeZoneInfo userTimeZone)
        {
            return dateTime == null ? null : ConvertToServerTimezone(dateTime.Value, userTimeZone);
        }

        public DateTime ConvertToServerTimezone(DateTime dateTime, TimeZoneInfo userTimeZone)
        {
            var unspecifiedDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
            var userTimeZoneOffset = userTimeZone.BaseUtcOffset;
            var dateTimeOffset = new DateTimeOffset(unspecifiedDateTime, userTimeZoneOffset);
            return TimeZoneInfo.ConvertTime(dateTimeOffset, ServerTimezone).DateTime;
        }
    }
}
