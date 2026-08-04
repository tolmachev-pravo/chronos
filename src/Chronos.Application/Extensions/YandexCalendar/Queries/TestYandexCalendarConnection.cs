using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Extensions.YandexCalendar.Queries
{
    public class TestYandexCalendarConnection
    {
        public record Query(string Login, string AppPassword) : IRequest<int>;

        public class Handler : IRequestHandler<Query, int>
        {
            private readonly IYandexCalendarService _calendar;

            public Handler(IYandexCalendarService calendar) => _calendar = calendar;

            public async Task<int> Handle(Query request, CancellationToken ct)
            {
                var events = await _calendar.GetEventsAsync(
                    new YandexCalendarCredentials(request.Login, request.AppPassword),
                    DateOnly.FromDateTime(DateTime.Today),
                    TimeZoneInfo.Local,
                    ct);
                return events.Count;
            }
        }
    }
}
