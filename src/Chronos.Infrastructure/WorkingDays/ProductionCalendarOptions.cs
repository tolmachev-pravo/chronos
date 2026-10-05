namespace Chronos.Infrastructure.WorkingDays
{
    /// <summary>
    /// The «ProductionCalendar» section: where the calendar is read from and how often.
    /// See issue #310.
    /// </summary>
    public class ProductionCalendarOptions
    {
        public const string SectionName = "ProductionCalendar";

        /// <summary>Off, the table keeps whatever it holds and nobody refreshes it.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>The address of a year's calendar; «{0}» is the year.</summary>
        public string Url { get; set; } = "https://xmlcalendar.ru/data/ru/{0}/calendar.json";

        /// <summary>How often the current and the next year are read again.</summary>
        public int RefreshIntervalHours { get; set; } = 24;

        public int TimeoutSeconds { get; set; } = 30;
    }
}
