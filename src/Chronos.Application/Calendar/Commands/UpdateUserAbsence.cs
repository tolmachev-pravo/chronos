using MediatR;
using Chronos.Domain.Entities.Calendar;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Calendar.Commands
{
    /// <summary>Changes the dates, the kind or the comment of an absence. See issue #310.</summary>
    public class UpdateUserAbsence
    {
        /// <summary>The username keeps a user to their own absences.</summary>
        public record Command(
            string Username,
            Guid Id,
            DateTime StartDate,
            DateTime EndDate,
            AbsenceKind Kind,
            string Comment = null) : IRequest<bool>;

        public class Handler : IRequestHandler<Command, bool>
        {
            private readonly IUserAbsenceRepository _repository;

            public Handler(IUserAbsenceRepository repository)
            {
                _repository = repository;
            }

            public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
            {
                await AbsenceOverlap.EnsureFreeAsync(
                    _repository, request.Username, request.StartDate.Date, request.EndDate.Date, request.Id, cancellationToken);

                return await _repository.UpdateAsync(new UserAbsence
                {
                    Id = request.Id,
                    Username = request.Username,
                    StartDate = request.StartDate.Date,
                    EndDate = request.EndDate.Date,
                    Kind = request.Kind,
                    Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
                    UpdatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
        }
    }
}
