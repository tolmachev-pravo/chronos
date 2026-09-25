using Chronos.Application.Worklogs.Queries;
using System;
using System.Globalization;

namespace Chronos.Web.Components.Worklogs
{
    /// <summary>
    /// The days a search covers, both ends inclusive. A period never runs past today:
    /// a future day has nothing to log yet and would only show up as a missing norm.
    /// </summary>
    public sealed record WorklogPeriod(DateTime Start, DateTime End, WorklogPeriodKind Kind)
    {
        private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

        public static WorklogPeriod ThisWeek(DateTime today) => WeekOf(today, today);

        public static WorklogPeriod LastWeek(DateTime today) => WeekOf(today.AddDays(-7), today);

        public static WorklogPeriod ThisMonth(DateTime today) =>
            MonthOf(new DateTime(today.Year, today.Month, 1), today);

        public static WorklogPeriod Custom(DateTime start, DateTime end, DateTime today) =>
            new(start.Date, Min(end.Date, today.Date), WorklogPeriodKind.Custom);

        /// <summary>The Monday-to-Sunday week the day falls in.</summary>
        public static WorklogPeriod WeekOf(DateTime day, DateTime today)
        {
            var monday = day.Date.AddDays(-(((int)day.DayOfWeek + 6) % 7));
            return new WorklogPeriod(monday, Min(monday.AddDays(6), today.Date), WorklogPeriodKind.Week);
        }

        private static WorklogPeriod MonthOf(DateTime first, DateTime today) =>
            new(first, Min(first.AddMonths(1).AddDays(-1), today.Date), WorklogPeriodKind.Month);

        /// <summary>
        /// The neighbouring period of the same kind: a week steps by weeks, a month by
        /// months, any other range by its own length. Stepping forward stops at today.
        /// </summary>
        public WorklogPeriod Shift(int steps, DateTime today)
        {
            if (Kind == WorklogPeriodKind.Week)
                return WeekOf(Start.AddDays(7 * steps), today);
            if (Kind == WorklogPeriodKind.Month)
                return MonthOf(Start.AddMonths(steps), today);
            var length = (End - Start).Days + 1;
            return Custom(Start.AddDays(length * steps), End.AddDays(length * steps), today);
        }

        public bool CanShiftForward(DateTime today) => End < today.Date;

        public bool Contains(DateTime day) => day.Date >= Start && day.Date <= End;

        /// <summary>«15 – 21 сентября», «29 сентября – 5 октября», «сентябрь 2026».</summary>
        public string Label
        {
            get
            {
                if (Kind == WorklogPeriodKind.Month)
                    return Start.ToString("MMMM yyyy", Russian);
                if (Start == End)
                    return Start.ToString("d MMMM", Russian);
                if (Start.Year != End.Year)
                    return $"{Start.ToString("d MMMM yyyy", Russian)} – {End.ToString("d MMMM yyyy", Russian)}";
                if (Start.Month != End.Month)
                    return $"{Start.ToString("d MMMM", Russian)} – {End.ToString("d MMMM", Russian)}";
                return $"{Start.Day} – {End.ToString("d MMMM", Russian)}";
            }
        }

        public GetWorklogCollection.Query ToQuery() => new()
        {
            StartDate = Start,
            EndDate = End.AddDays(1).AddMinutes(-1),
        };

        private static DateTime Min(DateTime left, DateTime right) => left < right ? left : right;
    }

    public enum WorklogPeriodKind
    {
        Week,
        Month,
        Custom
    }
}
