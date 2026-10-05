using System;
using System.Collections.Generic;

namespace Chronos.Application.Calendar
{
    /// <summary>
    /// The public holidays of the Labour Code, art. 112. Their dates are set by the law
    /// itself, unlike the days off carried over each year, and they are what a vacation is
    /// lengthened by: a public holiday inside a vacation does not use up a vacation day,
    /// a carried-over day off does. See issue #310.
    /// </summary>
    public static class PublicHolidays
    {
        private static readonly Dictionary<(int Month, int Day), string> Names = new()
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

        /// <summary>The holiday's name, or null when the date is not a public holiday.</summary>
        public static string NameOf(DateTime date) => Names.GetValueOrDefault((date.Month, date.Day));

        public static bool Contains(DateTime date) => Names.ContainsKey((date.Month, date.Day));
    }
}
