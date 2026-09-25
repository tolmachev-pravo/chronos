using NUnit.Framework;
using Chronos.Web.Components.Worklogs;
using System;

namespace Chronos.UnitTests.Web.Worklogs
{
    /// <summary>
    /// The periods the worklog page offers: weeks run Monday to Sunday, stepping keeps the
    /// kind of period, and nothing reaches past today.
    /// </summary>
    [TestFixture]
    public class WorklogPeriodTests
    {
        // Thursday.
        private static readonly DateTime Today = new(2026, 9, 24);

        [Test]
        public void ThisWeek_Should_StartOnMonday_AndStopAtToday()
        {
            var period = WorklogPeriod.ThisWeek(Today);

            Assert.That(period.Start, Is.EqualTo(new DateTime(2026, 9, 21)));
            Assert.That(period.End, Is.EqualTo(Today));
        }

        [Test]
        public void LastWeek_Should_BeTheWholePreviousWeek()
        {
            var period = WorklogPeriod.LastWeek(Today);

            Assert.That(period.Start, Is.EqualTo(new DateTime(2026, 9, 14)));
            Assert.That(period.End, Is.EqualTo(new DateTime(2026, 9, 20)));
            Assert.That(period.Label, Is.EqualTo("14 – 20 сентября"));
        }

        [Test]
        public void WeekOf_Should_TreatSundayAsTheEndOfTheWeek()
        {
            var period = WorklogPeriod.WeekOf(new DateTime(2026, 9, 20), Today);

            Assert.That(period.Start, Is.EqualTo(new DateTime(2026, 9, 14)));
        }

        [Test]
        public void Shift_Should_StepAWeekForward_UpToToday()
        {
            var period = WorklogPeriod.LastWeek(Today).Shift(1, Today);

            Assert.That(period, Is.EqualTo(WorklogPeriod.ThisWeek(Today)));
            Assert.That(period.CanShiftForward(Today), Is.False);
        }

        [Test]
        public void Shift_Should_StepByMonths_ForAMonth()
        {
            var period = WorklogPeriod.ThisMonth(Today).Shift(-1, Today);

            Assert.That(period.Start, Is.EqualTo(new DateTime(2026, 8, 1)));
            Assert.That(period.End, Is.EqualTo(new DateTime(2026, 8, 31)));
            Assert.That(period.Kind, Is.EqualTo(WorklogPeriodKind.Month));
        }

        [Test]
        public void Shift_Should_StepByItsOwnLength_ForACustomRange()
        {
            var period = WorklogPeriod.Custom(new DateTime(2026, 9, 1), new DateTime(2026, 9, 10), Today)
                .Shift(-1, Today);

            Assert.That(period.Start, Is.EqualTo(new DateTime(2026, 8, 22)));
            Assert.That(period.End, Is.EqualTo(new DateTime(2026, 8, 31)));
        }

        [Test]
        public void Label_Should_NameBothMonths_When_TheWeekCrossesThem()
        {
            var period = WorklogPeriod.WeekOf(new DateTime(2026, 9, 29), new DateTime(2026, 10, 10));

            Assert.That(period.Label, Is.EqualTo("28 сен – 4 окт"));
        }

        [Test]
        public void Label_Should_NameTheYears_When_TheWeekCrossesThem()
        {
            var period = WorklogPeriod.WeekOf(new DateTime(2026, 12, 31), new DateTime(2027, 1, 10));

            Assert.That(period.Label, Is.EqualTo("28 дек 26 – 3 янв 27"));
        }

        [Test]
        public void Label_Should_NameTheMonth_ForAMonth()
        {
            Assert.That(WorklogPeriod.ThisMonth(Today).Label, Is.EqualTo("Сентябрь 2026"));
        }

        [Test]
        public void ToQuery_Should_CoverTheLastDayWhole()
        {
            var query = WorklogPeriod.LastWeek(Today).ToQuery();

            Assert.That(query.StartDate, Is.EqualTo(new DateTime(2026, 9, 14)));
            Assert.That(query.EndDate, Is.EqualTo(new DateTime(2026, 9, 20, 23, 59, 0)));
        }
    }
}
