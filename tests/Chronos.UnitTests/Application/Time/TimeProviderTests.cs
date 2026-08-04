using Chronos.Application.Time;

namespace Chronos.UnitTests.Application.Time
{
    public class TimeProviderTests
    {
        private ITimeProvider _timeProvider;

        [SetUp]
        public void Setup()
        {
            _timeProvider = new Chronos.Application.Time.TimeProvider();
        }

        [Test]
        public void ConvertToUserTimezone_NullDateTime_ReturnsNull()
        {
            // Arrange
            var dateTime = (DateTime?)null;
            var userTimeZone = TimeZoneInfo.Local;

            // Act
            var result = _timeProvider.ConvertToUserTimezone(dateTime, userTimeZone);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ConvertToUserTimezone_DateTime_ReturnsCorrectDateTime()
        {
            // Arrange
            var dateTime = new DateTime(2022, 1, 1, 12, 0, 0);
            var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var serverTimeZone = TimeZoneInfo.Local;

            // Act
            var result = _timeProvider.ConvertToUserTimezone(dateTime, userTimeZone);

            // Assert
            Assert.That(result, Is.EqualTo(dateTime.AddTicks(-serverTimeZone.BaseUtcOffset.Ticks).AddTicks(userTimeZone.BaseUtcOffset.Ticks)));
        }

        [Test]
        public void ConvertToServerTimezone_NullDateTime_ReturnsNull()
        {
            // Arrange
            var dateTime = (DateTime?)null;
            var userTimeZone = TimeZoneInfo.Local;

            // Act
            var result = _timeProvider.ConvertToServerTimezone(dateTime, userTimeZone);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ConvertToServerTimezone_DateTime_ReturnsCorrectDateTime()
        {
            // Arrange
            var dateTime = new DateTime(2022, 1, 1, 12, 0, 0);
            var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var serverTimeZone = TimeZoneInfo.Local;

            // Act
            var result = _timeProvider.ConvertToServerTimezone(dateTime, userTimeZone);

            // Assert
            Assert.That(result, Is.EqualTo(dateTime.AddTicks(serverTimeZone.BaseUtcOffset.Ticks).AddTicks(-userTimeZone.BaseUtcOffset.Ticks)));
        }
    }
}