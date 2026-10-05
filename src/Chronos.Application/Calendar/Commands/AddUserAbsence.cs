using MediatR;
using Chronos.Domain.Entities.Calendar;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Calendar.Commands
{
    /// <summary>
    /// «Vacation from the 14th to the 27th of July» — one row for the whole stretch.
    /// See issue #310.
    /// </summary>
    public class AddUserAbsence
    {
        public record Command(
            string Username,
            DateTime StartDate,
            DateTime EndDate,
            AbsenceKind Kind,
            string Comment = null) : IRequest<Guid>;

        public class Handler : IRequestHandler<Command, Guid>
        {
            private readonly IUserAbsenceRepository _repository;

            public Handler(IUserAbsenceRepository repository)
            {
                _repository = repository;
            }

            public async Task<Guid> Handle(Command request, CancellationToken cancellationToken)
            {
                var absence = new UserAbsence
                {
                    Id = Guid.NewGuid(),
                    Username = request.Username,
                    StartDate = request.StartDate.Date,
                    EndDate = request.EndDate.Date,
                    Kind = request.Kind,
                    Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.AddAsync(absence, cancellationToken);
                return absence.Id;
            }
        }
    }
}
