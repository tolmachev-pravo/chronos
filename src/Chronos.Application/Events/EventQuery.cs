using System;

namespace Chronos.Application.Events
{
    /// <summary>
    /// The period a provider is asked for, on behalf of one user.
    /// </summary>
    public record EventQuery(string Username, DateTime StartDate, DateTime EndDate);
}
