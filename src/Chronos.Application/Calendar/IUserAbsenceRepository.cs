using Chronos.Domain.Entities.Calendar;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Calendar
{
    public interface IUserAbsenceRepository
    {
        /// <summary>The user's absences that touch the days from <paramref name="from"/> to <paramref name="to"/>.</summary>
        Task<IReadOnlyList<UserAbsence>> GetAsync(string username, DateTime from, DateTime to, CancellationToken ct = default);

        /// <summary>Every absence of the user, latest first.</summary>
        Task<IReadOnlyList<UserAbsence>> ListAsync(string username, CancellationToken ct = default);

        Task AddAsync(UserAbsence absence, CancellationToken ct = default);

        /// <summary>Removes the absence if it is the user's; false when there is no such absence.</summary>
        Task<bool> DeleteAsync(string username, Guid id, CancellationToken ct = default);
    }
}
