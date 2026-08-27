using MediatR;
using Chronos.Application.Users.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Users.Queries
{
    public class GetUserSettings
    {
        public record Query(string Username) : IRequest<UserSettingsDto>;

        public class Handler : IRequestHandler<Query, UserSettingsDto>
        {
            private readonly IUserSettingsRepository _repository;

            public Handler(IUserSettingsRepository repository)
            {
                _repository = repository;
            }

            /// <summary>
            /// A user without stored settings gets the defaults, so worklog search keeps
            /// working before the settings are seeded or opened for the first time.
            /// </summary>
            public async Task<UserSettingsDto> Handle(Query request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(request.Username))
                    return UserSettingsDto.Default;

                var entity = await _repository.GetAsync(request.Username, cancellationToken);
                if (entity is null)
                    return UserSettingsDto.Default;

                return new UserSettingsDto(
                    entity.WorkingStartTime,
                    entity.WorkingEndTime,
                    entity.LunchTime);
            }
        }
    }
}
