using Chronos.Application.Events;
using Chronos.Domain.Models.Events;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Mock
{
    /// <summary>
    /// Serves the fixture events of one source. Registered once per source, so the mock
    /// mode exercises the same multi-provider path as the real one. See issue #299.
    /// </summary>
    internal class MockEventProvider : IEventProvider
    {
        public MockEventProvider(EventSource source)
        {
            Source = source;
        }

        public EventSource Source { get; }

        public Task<bool> PrepareAsync(EventQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<IEnumerable<IEvent>> GetEventsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(MockWorklogStorage.Events
                .Where(userEvent => userEvent.Source == Source)
                .Cast<IEvent>()
                .ToList()
                .AsEnumerable());
    }
}
