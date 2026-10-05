using Moq;
using Chronos.Application.Calendar;
using Chronos.Application.Calendar.Commands;
using Chronos.Domain.Entities.Calendar;

namespace Chronos.UnitTests.Application.Calendar
{
    /// <summary>A day is one kind of absence at most. See issue #310.</summary>
    [TestFixture]
    public class UserAbsenceCommandsTests
    {
        private Mock<IUserAbsenceRepository> _repository;

        [SetUp]
        public void SetUp()
        {
            _repository = new Mock<IUserAbsenceRepository>();
            _repository
                .Setup(repository => repository.GetAsync("john", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<UserAbsence>());
            _repository
                .Setup(repository => repository.UpdateAsync(It.IsAny<UserAbsence>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        private void SetUpStored(UserAbsence absence) =>
            _repository
                .Setup(repository => repository.GetAsync("john", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { absence });

        private static UserAbsence Stored(Guid id) => new()
        {
            Id = id,
            Username = "john",
            StartDate = new DateTime(2026, 7, 13),
            EndDate = new DateTime(2026, 7, 26),
            Kind = AbsenceKind.Vacation
        };

        [Test]
        public void Add_OverlappingAnotherAbsence_IsRefused()
        {
            SetUpStored(Stored(Guid.NewGuid()));
            var handler = new AddUserAbsence.Handler(_repository.Object);

            Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
                new AddUserAbsence.Command("john", new DateTime(2026, 7, 20), new DateTime(2026, 7, 21), AbsenceKind.SickLeave),
                CancellationToken.None));
            _repository.Verify(repository => repository.AddAsync(It.IsAny<UserAbsence>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Update_MovingWithinItsOwnDates_IsAllowed()
        {
            var id = Guid.NewGuid();
            SetUpStored(Stored(id));
            var handler = new UpdateUserAbsence.Handler(_repository.Object);

            var updated = await handler.Handle(
                new UpdateUserAbsence.Command("john", id, new DateTime(2026, 7, 14), new DateTime(2026, 7, 27), AbsenceKind.Vacation, "  Море "),
                CancellationToken.None);

            Assert.That(updated, Is.True);
            _repository.Verify(repository => repository.UpdateAsync(
                It.Is<UserAbsence>(absence => absence.Id == id
                    && absence.Username == "john"
                    && absence.StartDate == new DateTime(2026, 7, 14)
                    && absence.EndDate == new DateTime(2026, 7, 27)
                    && absence.Comment == "Море"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Update_OntoAnotherAbsence_IsRefused()
        {
            SetUpStored(Stored(Guid.NewGuid()));
            var handler = new UpdateUserAbsence.Handler(_repository.Object);

            Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
                new UpdateUserAbsence.Command("john", Guid.NewGuid(), new DateTime(2026, 7, 1), new DateTime(2026, 7, 13), AbsenceKind.DayOff),
                CancellationToken.None));
        }
    }
}
