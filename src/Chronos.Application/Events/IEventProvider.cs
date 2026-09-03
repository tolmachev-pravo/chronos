using Chronos.Domain.Models.Events;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Events
{
    /// <summary>
    /// One source of user events. Adding a source means adding a provider and
    /// registering it — nothing in the day assembly changes. See issue #299.
    ///
    /// The two phases exist because the scoped ApplicationDbContext is not
    /// thread-safe: settings are read in the sequential PrepareAsync, so the parallel
    /// fetch phase does not touch it. See issue #258.
    /// </summary>
    public interface IEventProvider
    {
        EventSource Source { get; }

        /// <summary>
        /// Sequential phase: reads the user's settings and caches what the fetch needs.
        /// Returns false when the source is switched off — GetEventsAsync is then never
        /// called and the external system is not queried at all. See issue #242.
        /// </summary>
        Task<bool> PrepareAsync(EventQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Parallel phase: external calls only. Runs after a PrepareAsync that
        /// returned true.
        /// </summary>
        Task<IEnumerable<IEvent>> GetEventsAsync(CancellationToken cancellationToken = default);
    }
}
