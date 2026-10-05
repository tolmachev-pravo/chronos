using Chronos.Domain.Entities.Calendar;
using System;

namespace Chronos.Application.Calendar.Dto
{
    /// <summary>An absence of the user, from its first day to its last, both inclusive.</summary>
    public record UserAbsenceDto(
        Guid Id,
        DateTime StartDate,
        DateTime EndDate,
        AbsenceKind Kind,
        string Comment)
    {
        public int Days => (EndDate.Date - StartDate.Date).Days + 1;

        public static UserAbsenceDto From(UserAbsence absence) => new(
            absence.Id,
            absence.StartDate.Date,
            absence.EndDate.Date,
            absence.Kind,
            absence.Comment);
    }
}
