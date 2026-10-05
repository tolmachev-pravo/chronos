using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Calendar.Commands
{
    /// <summary>
    /// A day is one kind of absence at most: a day both on vacation and on sick leave would
    /// leave the calendar to pick one at random. See issue #310.
    /// </summary>
    internal static class AbsenceOverlap
    {
        /// <param name="ignoredId">The absence being changed — it may overlap its own old dates.</param>
        public static async Task EnsureFreeAsync(
            IUserAbsenceRepository repository,
            string username,
            DateTime startDate,
            DateTime endDate,
            Guid? ignoredId,
            CancellationToken ct)
        {
            var overlapping = (await repository.GetAsync(username, startDate, endDate, ct))
                .FirstOrDefault(absence => absence.Id != ignoredId);

            if (overlapping is not null)
                throw new InvalidOperationException(
                    $"Эти даты пересекаются с отсутствием {overlapping.StartDate:dd.MM.yyyy}–{overlapping.EndDate:dd.MM.yyyy}");
        }
    }
}
