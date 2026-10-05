using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Calendar.Queries
{
    /// <summary>
    /// What each day of a period is for the user. Cheap — it reads only our own tables —
    /// so the page can ask for it before the period itself is read. See issue #310.
    /// </summary>
    public class GetWorkingCalendar
    {
        public record Query(string Username, DateTime From, DateTime To)
            : IRequest<IReadOnlyDictionary<DateTime, WorkingCalendarDay>>;

        public class Handler : IRequestHandler<Query, IReadOnlyDictionary<DateTime, WorkingCalendarDay>>
        {
            private readonly IWorkingCalendar _calendar;

            public Handler(IWorkingCalendar calendar)
            {
                _calendar = calendar;
            }

            public Task<IReadOnlyDictionary<DateTime, WorkingCalendarDay>> Handle(
                Query request, CancellationToken cancellationToken) =>
                _calendar.GetDaysAsync(request.Username, request.From, request.To, cancellationToken);
        }
    }
}
