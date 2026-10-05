using Chronos.Domain.Entities.Calendar;
using Chronos.Infrastructure.WorkingDays;

namespace Chronos.UnitTests.Infrastructure.WorkingDays
{
    /// <summary>
    /// The calendars are xmlcalendar.ru's own files for 2025 and 2026, as published.
    /// See issue #310.
    /// </summary>
    [TestFixture]
    public class XmlCalendarParserTests
    {
        private const string Year2026 = """
            {
              "year": 2026,
              "months": [
                {"month": 1, "days": "1,2,3,4,5,6,7,8,9+,10,11,17,18,24,25,31"},
                {"month": 2, "days": "1,7,8,14,15,21,22,23,28"},
                {"month": 3, "days": "1,7,8,9+,14,15,21,22,28,29"},
                {"month": 4, "days": "4,5,11,12,18,19,25,26,30*"},
                {"month": 5, "days": "1,2,3,8*,9,10,11+,16,17,23,24,30,31"},
                {"month": 6, "days": "6,7,11*,12,13,14,20,21,27,28"},
                {"month": 7, "days": "4,5,11,12,18,19,25,26"},
                {"month": 8, "days": "1,2,8,9,15,16,22,23,29,30"},
                {"month": 9, "days": "5,6,12,13,19,20,26,27"},
                {"month": 10, "days": "3,4,10,11,17,18,24,25,31"},
                {"month": 11, "days": "1,3*,4,7,8,14,15,21,22,28,29"},
                {"month": 12, "days": "5,6,12,13,19,20,26,27,31+"}
              ],
              "transitions": [
                {"from": "01.03", "to": "01.09"},
                {"from": "03.08", "to": "03.09"},
                {"from": "05.09", "to": "05.11"},
                {"from": "01.04", "to": "12.31"}
              ]
            }
            """;

        private const string Year2025 = """
            {
              "year": 2025,
              "months": [
                {"month": 1, "days": "1,2,3,4,5,6,7,8,11,12,18,19,25,26"},
                {"month": 2, "days": "1,2,8,9,15,16,22,23"},
                {"month": 3, "days": "1,2,7*,8,9,15,16,22,23,29,30"},
                {"month": 4, "days": "5,6,12,13,19,20,26,27,30*"},
                {"month": 5, "days": "1,2+,3,4,8+,9,10,11,17,18,24,25,31"},
                {"month": 6, "days": "1,7,8,11*,12,13+,14,15,21,22,28,29"},
                {"month": 7, "days": "5,6,12,13,19,20,26,27"},
                {"month": 8, "days": "2,3,9,10,16,17,23,24,30,31"},
                {"month": 9, "days": "6,7,13,14,20,21,27,28"},
                {"month": 10, "days": "4,5,11,12,18,19,25,26"},
                {"month": 11, "days": "1*,2,3+,4,8,9,15,16,22,23,29,30"},
                {"month": 12, "days": "6,7,13,14,20,21,27,28,31+"}
              ],
              "transitions": [
                {"from": "01.04", "to": "05.02"},
                {"from": "02.23", "to": "05.08"},
                {"from": "03.08", "to": "06.13"},
                {"from": "11.01", "to": "11.03"},
                {"from": "01.05", "to": "12.31"}
              ]
            }
            """;

        private static Dictionary<DateTime, CalendarDay> Parse(string json) =>
            XmlCalendarParser.Parse(json).ToDictionary(day => day.Date);

        [Test]
        public void Parse_WeekdayHoliday_IsHolidayWithItsName()
        {
            var days = Parse(Year2026);

            Assert.That(days[new DateTime(2026, 6, 12)].Kind, Is.EqualTo(CalendarDayKind.Holiday));
            Assert.That(days[new DateTime(2026, 6, 12)].Title, Is.EqualTo("День России"));
            Assert.That(days[new DateTime(2026, 1, 7)].Title, Is.EqualTo("Рождество Христово"));
            Assert.That(days[new DateTime(2026, 2, 23)].Title, Is.EqualTo("День защитника Отечества"));
        }

        [Test]
        public void Parse_CarriedOverDayOff_IsHolidaySayingWhereItCameFrom()
        {
            var days = Parse(Year2026);

            Assert.That(days[new DateTime(2026, 1, 9)].Kind, Is.EqualTo(CalendarDayKind.Holiday));
            Assert.That(days[new DateTime(2026, 1, 9)].Title, Is.EqualTo("Перенесённый выходной (с 3 января)"));
            Assert.That(days[new DateTime(2026, 3, 9)].Title, Is.EqualTo("Перенесённый выходной (с 8 марта)"));
            Assert.That(days[new DateTime(2026, 12, 31)].Kind, Is.EqualTo(CalendarDayKind.Holiday));
        }

        [Test]
        public void Parse_PreHolidayDay_IsShortDay()
        {
            var days = Parse(Year2026);

            foreach (var date in new[] { new DateTime(2026, 4, 30), new DateTime(2026, 5, 8), new DateTime(2026, 6, 11), new DateTime(2026, 11, 3) })
                Assert.That(days[date].Kind, Is.EqualTo(CalendarDayKind.ShortDay), date.ToString("d"));
        }

        [Test]
        public void Parse_HolidayOnWeekend_IsKeptForItsName()
        {
            var days = Parse(Year2026);

            // Sunday, 8 March 2026.
            Assert.That(days[new DateTime(2026, 3, 8)].Kind, Is.EqualTo(CalendarDayKind.Holiday));
            Assert.That(days[new DateTime(2026, 3, 8)].Title, Is.EqualTo("Международный женский день"));
        }

        [Test]
        public void Parse_OrdinaryDays_AreNotStored()
        {
            var days = Parse(Year2026);

            Assert.That(days.ContainsKey(new DateTime(2026, 6, 2)), Is.False, "an ordinary Tuesday");
            Assert.That(days.ContainsKey(new DateTime(2026, 6, 6)), Is.False, "an ordinary Saturday");
            Assert.That(days.ContainsKey(new DateTime(2026, 1, 10)), Is.False, "a Saturday with no holiday of its own");
        }

        [Test]
        public void Parse_Year2026_HasNoWorkingWeekends()
        {
            var days = Parse(Year2026);

            Assert.That(days.Values.Where(day => day.Kind == CalendarDayKind.Workday), Is.Empty);
        }

        [Test]
        public void Parse_WorkingSaturdayBeforeHoliday_IsShortDay()
        {
            var days = Parse(Year2025);

            // Saturday, 1 November 2025: worked for Monday the 3rd, and an hour shorter.
            Assert.That(days[new DateTime(2025, 11, 1)].Kind, Is.EqualTo(CalendarDayKind.ShortDay));
            Assert.That(days[new DateTime(2025, 11, 3)].Kind, Is.EqualTo(CalendarDayKind.Holiday));
            Assert.That(days[new DateTime(2025, 11, 3)].Title, Is.EqualTo("Перенесённый выходной (с 1 ноября)"));
        }

        [Test]
        public void Parse_EveryDay_HasATitle()
        {
            Assert.That(XmlCalendarParser.Parse(Year2025).Concat(XmlCalendarParser.Parse(Year2026)),
                Has.All.Matches<CalendarDay>(day => !string.IsNullOrEmpty(day.Title)));
        }

        [Test]
        public void Parse_WorkingWeekendMissingFromTheList_IsWorkday()
        {
            const string json = """
                {"year": 2024, "months": [{"month": 12, "days": "1,7,8,14,15,21,22,29,30,31"}],
                 "transitions": [{"from": "12.28", "to": "12.30"}]}
                """;

            var days = Parse(json);

            // Saturday, 28 December 2024, worked for Monday the 30th.
            Assert.That(days[new DateTime(2024, 12, 28)].Kind, Is.EqualTo(CalendarDayKind.Workday));
            Assert.That(days[new DateTime(2024, 12, 28)].Title, Is.EqualTo("Рабочий день (выходной перенесён на 30 декабря)"));
        }
    }
}
