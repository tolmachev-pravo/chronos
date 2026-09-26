using MudBlazor;
using System;
using System.Globalization;

namespace Chronos.Web.Components.Worklogs.Summary
{
    /// <summary>How the summary writes its numbers and names what the time went to.</summary>
    public static class SummaryFormat
    {
        private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

        private static readonly string[] Weekdays = { "пн", "вт", "ср", "чт", "пт", "сб", "вс" };

        /// <summary>«8 ч», «7,5 ч», «0 ч».</summary>
        public static string Hours(TimeSpan value) =>
            $"{Math.Round(value.TotalHours, 1).ToString("0.#", Russian)} ч";

        public static string Number(double value) => Math.Round(value, 1).ToString("0.#", Russian);

        public static string Percent(double share) => $"{Math.Round(share * 100)}%";

        /// <summary>Monday is 0.</summary>
        public static string Weekday(int weekday) => Weekdays[weekday];

        public static string Weekday(DateTime date) => Weekdays[((int)date.DayOfWeek + 6) % 7];

        /// <summary>«пн, 21 сентября» — for tooltips.</summary>
        public static string Date(DateTime date) => $"{Weekday(date)}, {date.ToString("d MMMM", Russian)}";

        public static string Time(TimeSpan value) => value.ToString(@"hh\:mm");

        public static string Name(SummaryKind kind) => kind switch
        {
            SummaryKind.Work => "Работа в Jira",
            SummaryKind.Testing => "Тестирование",
            SummaryKind.Comment => "Комментарии",
            SummaryKind.Calendar => "Календарь",
            _ => "Вручную"
        };

        /// <summary>The same icons the day rows use for their sources.</summary>
        public static string Icon(SummaryKind kind) => kind switch
        {
            SummaryKind.Work => Icons.Material.Filled.Assignment,
            SummaryKind.Testing => Icons.Material.Filled.BugReport,
            SummaryKind.Comment => Icons.Material.Filled.Comment,
            SummaryKind.Calendar => Icons.Material.Filled.Event,
            _ => Icons.Material.Filled.Edit
        };

        public static string Class(SummaryKind kind) => $"chr-summary-kind--{kind.ToString().ToLowerInvariant()}";
    }
}
