using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Calendar.Commands
{
    public class DeleteUserAbsence
    {
        /// <summary>The username keeps a user to their own absences.</summary>
        public record Command(string Username, Guid Id) : IRequest<bool>;

        public class Handler : IRequestHandler<Command, bool>
        {
            private readonly IUserAbsenceRepository _repository;

            public Handler(IUserAbsenceRepository repository)
            {
                _repository = repository;
            }

            public Task<bool> Handle(Command request, CancellationToken cancellationToken) =>
                _repository.DeleteAsync(request.Username, request.Id, cancellationToken);
        }
    }
}
