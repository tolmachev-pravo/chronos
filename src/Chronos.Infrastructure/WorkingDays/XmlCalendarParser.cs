using Chronos.Domain.Entities.Calendar;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronos.Infrastructure.WorkingDays
{
    /// <summary>
    /// Reads a year of the Russian production calendar as xmlcalendar.ru publishes it and
    /// keeps only the exceptions to the weekday rule. See issue #310.
    ///
    /// Each month lists every day off; a day marked «*» is a shortened working day before a
    /// holiday, and «+» a day off carried over from elsewhere. A weekend missing from the
    /// list is a working Saturday or Sunday. «transitions» pair the day a day off moved from
    /// with the day it moved to, as «MM.dd».
    /// </summary>
    public static class XmlCalendarParser
    {
        private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

        /// <summary>The public holidays of the Labour Code, art. 112, by month and day.</summary>
        private static readonly Dictionary<(int Month, int Day), string> HolidayNames = new()
        {
            [(1, 1)] = "Новогодние каникулы",
            [(1, 2)] = "Новогодние каникулы",
            [(1, 3)] = "Новогодние каникулы",
            [(1, 4)] = "Новогодние каникулы",
            [(1, 5)] = "Новогодние каникулы",
            [(1, 6)] = "Новогодние каникулы",
            [(1, 7)] = "Рождество Христово",
            [(1, 8)] = "Новогодние каникулы",
            [(2, 23)] = "День защитника Отечества",
            [(3, 8)] = "Международный женский день",
            [(5, 1)] = "Праздник Весны и Труда",
            [(5, 9)] = "День Победы",
            [(6, 12)] = "День России",
            [(11, 4)] = "День народного единства",
        };

        public static IReadOnlyList<CalendarDay> Parse(string json)
        {
            var document = JsonSerializer.Deserialize<Document>(json)
                ?? throw new FormatException("The production calendar is empty.");
            var year = document.Year;

            var listed = new Dictionary<DateTime, char?>();
            foreach (var month in document.Months ?? new List<Month>())
            {
                foreach (var token in (month.Days ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var text = token.Trim();
                    char? mark = text.Length > 0 && !char.IsDigit(text[^1]) ? text[^1] : null;
                    var number = mark is null ? text : text[..^1];
                    listed[new DateTime(year, month.Number, int.Parse(number, CultureInfo.InvariantCulture))] = mark;
                }
            }

            var transitions = (document.Transitions ?? new List<Transition>())
                .Select(transition => (From: DateOf(year, transition.From), To: DateOf(year, transition.To)))
                .ToList();

            var days = new List<CalendarDay>();
            for (var date = new DateTime(year, 1, 1); date.Year == year; date = date.AddDays(1))
            {
                var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                var isListed = listed.TryGetValue(date, out var mark);
                var name = HolidayNames.GetValueOrDefault((date.Month, date.Day));

                if (isListed && mark == '*')
                {
                    days.Add(Day(date, CalendarDayKind.ShortDay, "Предпраздничный день"));
                }
                else if (isListed && !isWeekend)
                {
                    var movedFrom = transitions.FirstOrDefault(transition => transition.To == date).From;
                    var carriedOver = movedFrom != default
                        ? $"Перенесённый выходной (с {movedFrom.ToString("d MMMM", Russian)})"
                        : null;
                    var title = mark == '+'
                        ? carriedOver ?? name
                        : name ?? carriedOver;
                    days.Add(Day(date, CalendarDayKind.Holiday, title ?? "Нерабочий день"));
                }
                else if (isListed && name is not null)
                {
                    // A holiday on a weekend is a day off either way; it is kept for its name.
                    days.Add(Day(date, CalendarDayKind.Holiday, name));
                }
                else if (!isListed && isWeekend)
                {
                    var movedTo = transitions.FirstOrDefault(transition => transition.From == date).To;
                    var title = movedTo != default
                        ? $"Рабочий день (выходной перенесён на {movedTo.ToString("d MMMM", Russian)})"
                        : "Рабочий выходной";
                    days.Add(Day(date, CalendarDayKind.Workday, title));
                }
            }

            return days;
        }

        private static CalendarDay Day(DateTime date, CalendarDayKind kind, string title) => new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            Kind = kind,
            Title = title,
            CreatedAt = DateTime.UtcNow
        };

        private static DateTime DateOf(int year, string monthDay)
        {
            var parts = monthDay.Split('.');
            return new DateTime(
                year,
                int.Parse(parts[0], CultureInfo.InvariantCulture),
                int.Parse(parts[1], CultureInfo.InvariantCulture));
        }

        private sealed class Document
        {
            [JsonPropertyName("year")] public int Year { get; set; }
            [JsonPropertyName("months")] public List<Month> Months { get; set; }
            [JsonPropertyName("transitions")] public List<Transition> Transitions { get; set; }
        }

        private sealed class Month
        {
            [JsonPropertyName("month")] public int Number { get; set; }
            [JsonPropertyName("days")] public string Days { get; set; }
        }

        private sealed class Transition
        {
            [JsonPropertyName("from")] public string From { get; set; }
            [JsonPropertyName("to")] public string To { get; set; }
        }
    }
}
