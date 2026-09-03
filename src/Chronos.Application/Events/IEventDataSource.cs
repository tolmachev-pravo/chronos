using Chronos.Domain.Models.Events;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Events
{
    public interface IEventDataSource
    {
        Task<IEnumerable<IEvent>> GetEventsAsync(
            EventQuery query,
            CancellationToken cancellationToken = default);
    }
}
