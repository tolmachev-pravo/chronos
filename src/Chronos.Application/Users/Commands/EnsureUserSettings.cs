using MediatR;
using Chronos.Application.Users.Dto;
using Chronos.Domain.Entities.Users;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Users.Commands
{
    /// <summary>
    /// Creates the settings row for a user that has none yet — called right after login, so
    /// the profile always shows real values. Idempotent: an existing row is never touched.
    /// </summary>
    public class EnsureUserSettings
    {
        /// <param name="LegacySettings">
        /// Working day taken from the locally stored worklog filter, i.e. from the user's own
        /// answers before the settings moved to the profile (issue #241). Null for a user
        /// without such a filter — they get the defaults.
        /// </param>
        public record Command(string Username, UserSettingsDto LegacySettings) : IRequest;

        public class Handler : IRequestHandler<Command>
        {
            private readonly IUserSettingsRepository _repository;

            public Handler(IUserSettingsRepository repository)
            {
                _repository = repository;
            }

            public async Task Handle(Command request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(request.Username))
                    return;

                var existing = await _repository.GetAsync(request.Username, cancellationToken);
                if (existing is not null)
                    return;

                var settings = request.LegacySettings ?? UserSettingsDto.Default;

                await _repository.UpsertAsync(
                    new UserSettings
                    {
                        Username = request.Username,
                        WorkingStartTime = settings.WorkingStartTime,
                        WorkingEndTime = settings.WorkingEndTime,
                        LunchTime = settings.LunchTime,
                        CreatedAt = DateTime.UtcNow
                    },
                    cancellationToken);
            }
        }
    }
}
