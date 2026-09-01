using Chronos.Application.Worklogs.Dto;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Chronos.UnitTests.Application.Worklogs
{
    public class WorkingDaySettingsTests
    {
        public class WorkingTimeTestCases : IEnumerable
        {
            public IEnumerator GetEnumerator()
            {
                yield return new object[] { TimeSpan.FromHours(9), TimeSpan.FromHours(18), TimeSpan.FromHours(1), TimeSpan.FromHours(8) };
                yield return new object[] { TimeSpan.FromHours(9), TimeSpan.FromHours(18), TimeSpan.FromHours(0), TimeSpan.FromHours(9) };
                yield return new object[] { TimeSpan.FromHours(17), TimeSpan.FromHours(18), TimeSpan.FromHours(1), TimeSpan.FromHours(0) };
                yield return new object[] { TimeSpan.FromHours(18), TimeSpan.FromHours(18), TimeSpan.FromHours(1), TimeSpan.FromHours(-1) };
            }
        }

        [TestCaseSource(typeof(WorkingTimeTestCases))]
        public void WorkingTime_Should_BeCorrect(
            TimeSpan workingStartTime,
            TimeSpan workingEndTime,
            TimeSpan lunchTime,
            TimeSpan expectedWorkingTime)
        {
            // Arrange
            var settings = new WorkingDaySettings(
                workingStartTime: workingStartTime,
                workingEndTime: workingEndTime,
                lunchTime: lunchTime);

            // Act
            var workingTime = settings.WorkingTime;

            // Assert
            Assert.That(workingTime, Is.EqualTo(expectedWorkingTime));
        }

        [Test]
        public void Validate_Should_ValidateThatEndTimeGreaterThanStartTime()
        {
            // Arrange
            var settings = new WorkingDaySettings(
                workingStartTime: TimeSpan.FromHours(9),
                workingEndTime: TimeSpan.FromHours(8),
                lunchTime: TimeSpan.FromHours(1));

            // Act
            var validationResults = settings.Validate(new ValidationContext(settings));

            // Assert
            Assert.That(validationResults.Count(), Is.EqualTo(1));
        }
    }
}
