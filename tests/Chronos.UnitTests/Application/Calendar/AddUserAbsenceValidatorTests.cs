using Chronos.Application.Calendar.Commands;
using Chronos.Domain.Entities.Calendar;

namespace Chronos.UnitTests.Application.Calendar
{
    [TestFixture]
    public class AddUserAbsenceValidatorTests
    {
        private readonly AddUserAbsenceValidator _validator = new();

        [Test]
        public void Validate_OneDayAbsence_IsValid()
        {
            var command = new AddUserAbsence.Command("john", new DateTime(2026, 7, 14), new DateTime(2026, 7, 14), AbsenceKind.DayOff);

            Assert.That(_validator.Validate(command).IsValid, Is.True);
        }

        [Test]
        public void Validate_EndBeforeStart_IsInvalid()
        {
            var command = new AddUserAbsence.Command("john", new DateTime(2026, 7, 27), new DateTime(2026, 7, 14), AbsenceKind.Vacation);

            Assert.That(_validator.Validate(command).IsValid, Is.False);
        }

        [Test]
        public void Validate_WithoutUser_IsInvalid()
        {
            var command = new AddUserAbsence.Command("", new DateTime(2026, 7, 14), new DateTime(2026, 7, 27), AbsenceKind.Vacation);

            Assert.That(_validator.Validate(command).IsValid, Is.False);
        }
    }
}
