using MediatR;
using Chronos.Application.Users.Dto;
using Chronos.Domain.Entities.Users;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Users.Commands
{
    public class UpsertUserSettings
    {
        public record Command(string Username, UserSettingsDto Settings) : IRequest;

        public class Handler : IRequestHandler<Command>
        {
            private readonly IUserSettingsRepository _repository;

            public Handler(IUserSettingsRepository repository)
            {
                _repository = repository;
            }

            public async Task Handle(Command request, CancellationToken cancellationToken)
            {
                var existing = await _repository.GetAsync(request.Username, cancellationToken);

                var entity = existing ?? new UserSettings
                {
                    Username = request.Username,
                    CreatedAt = DateTime.UtcNow
                };

                entity.WorkingStartTime = request.Settings.WorkingStartTime;
                entity.WorkingEndTime = request.Settings.WorkingEndTime;
                entity.LunchTime = request.Settings.LunchTime;
                entity.UpdatedAt = DateTime.UtcNow;

                await _repository.UpsertAsync(entity, cancellationToken);
            }
        }
    }
}
