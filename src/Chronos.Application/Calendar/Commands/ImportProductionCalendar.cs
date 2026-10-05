using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Calendar.Commands
{
    /// <summary>
    /// Reads a year of the production calendar from its source into our table. The year is
    /// replaced as a whole, so a decree that moves a holiday reaches us on the next import.
    /// See issue #310.
    /// </summary>
    public class ImportProductionCalendar
    {
        /// <returns>How many exceptions the year holds, or null while the year is not published.</returns>
        public record Command(int Year) : IRequest<int?>;

        public class Handler : IRequestHandler<Command, int?>
        {
            private readonly IProductionCalendarSource _source;
            private readonly ICalendarDayRepository _repository;

            public Handler(IProductionCalendarSource source, ICalendarDayRepository repository)
            {
                _source = source;
                _repository = repository;
            }

            public async Task<int?> Handle(Command request, CancellationToken cancellationToken)
            {
                var days = await _source.GetYearAsync(request.Year, cancellationToken);

                // Every published year has holidays. An empty answer is a broken source,
                // and taking it at its word would wipe the year we already have.
                if (days is null || days.Count == 0)
                    return null;

                await _repository.ReplaceYearAsync(request.Year, days, cancellationToken);
                return days.Count;
            }
        }
    }
}
