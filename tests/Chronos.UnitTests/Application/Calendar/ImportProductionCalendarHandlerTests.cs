using Moq;
using Chronos.Application.Calendar;
using Chronos.Application.Calendar.Commands;
using Chronos.Domain.Entities.Calendar;

namespace Chronos.UnitTests.Application.Calendar
{
    [TestFixture]
    public class ImportProductionCalendarHandlerTests
    {
        private Mock<IProductionCalendarSource> _source;
        private Mock<ICalendarDayRepository> _repository;
        private ImportProductionCalendar.Handler _sut;

        [SetUp]
        public void SetUp()
        {
            _source = new Mock<IProductionCalendarSource>();
            _repository = new Mock<ICalendarDayRepository>();
            _sut = new ImportProductionCalendar.Handler(_source.Object, _repository.Object);
        }

        [Test]
        public async Task Handle_PublishedYear_ReplacesTheYear()
        {
            var days = new List<CalendarDay>
            {
                new() { Date = new DateTime(2026, 6, 12), Kind = CalendarDayKind.Holiday, Title = "День России" }
            };
            _source.Setup(source => source.GetYearAsync(2026, It.IsAny<CancellationToken>())).ReturnsAsync(days);

            var count = await _sut.Handle(new ImportProductionCalendar.Command(2026), CancellationToken.None);

            Assert.That(count, Is.EqualTo(1));
            _repository.Verify(repository => repository.ReplaceYearAsync(2026, days, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_YearNotPublished_KeepsWhatTheTableHolds()
        {
            _source.Setup(source => source.GetYearAsync(2027, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<CalendarDay>)null!);

            var count = await _sut.Handle(new ImportProductionCalendar.Command(2027), CancellationToken.None);

            Assert.That(count, Is.Null);
            _repository.Verify(repository => repository.ReplaceYearAsync(
                It.IsAny<int>(), It.IsAny<IReadOnlyCollection<CalendarDay>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_EmptyYear_DoesNotWipeTheYear()
        {
            _source.Setup(source => source.GetYearAsync(2026, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CalendarDay>());

            var count = await _sut.Handle(new ImportProductionCalendar.Command(2026), CancellationToken.None);

            Assert.That(count, Is.Null);
            _repository.Verify(repository => repository.ReplaceYearAsync(
                It.IsAny<int>(), It.IsAny<IReadOnlyCollection<CalendarDay>>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
