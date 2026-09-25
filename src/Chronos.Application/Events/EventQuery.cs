using System;

namespace Chronos.Application.Events
{
    /// <summary>
    /// The period a provider is asked for, on behalf of one user.
    /// </summary>
    public record EventQuery(string Username, DateTime StartDate, DateTime EndDate)
    {
        /// <summary>
        /// Optional listener told about each source as it starts and settles. Providers do
        /// not see it: the data source reports on their behalf.
        /// </summary>
        public IProgress<EventSourceProgress> Progress { get; init; }
    }
}
