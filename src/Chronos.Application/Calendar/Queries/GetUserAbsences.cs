using MediatR;
using Chronos.Application.Calendar.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Calendar.Queries
{
    public class GetUserAbsences
    {
        public record Query(string Username) : IRequest<IReadOnlyList<UserAbsenceDto>>;

        public class Handler : IRequestHandler<Query, IReadOnlyList<UserAbsenceDto>>
        {
            private readonly IUserAbsenceRepository _repository;

            public Handler(IUserAbsenceRepository repository)
            {
                _repository = repository;
            }

            public async Task<IReadOnlyList<UserAbsenceDto>> Handle(Query request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(request.Username))
                    return Array.Empty<UserAbsenceDto>();

                var absences = await _repository.ListAsync(request.Username, cancellationToken);
                return absences.Select(UserAbsenceDto.From).ToList();
            }
        }
    }
}
