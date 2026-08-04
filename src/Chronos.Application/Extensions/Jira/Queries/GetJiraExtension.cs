using MediatR;
using Chronos.Application.Extensions.Jira.Dto;
using Chronos.Domain.Entities.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Extensions.Jira.Queries
{
    public class GetJiraExtension
    {
        public record Query(string Username) : IRequest<JiraExtensionDto>;

        public class Handler : IRequestHandler<Query, JiraExtensionDto>
        {
            private readonly IUserExtensionRepository _repository;

            public Handler(IUserExtensionRepository repository)
            {
                _repository = repository;
            }

            /// <summary>
            /// A user without a stored extension is treated as connected with the default
            /// settings, so worklog search keeps working before the extension is seeded.
            /// </summary>
            public async Task<JiraExtensionDto> Handle(Query request, CancellationToken cancellationToken)
            {
                var entity = await _repository.GetAsync(request.Username, ExtensionType.Jira, cancellationToken);
                if (entity is null)
                    return new JiraExtensionDto(true, JiraExtensionSettingsDto.Default);

                return new JiraExtensionDto(
                    entity.IsEnabled,
                    JiraExtensionSettingsSerializer.Deserialize(entity.Settings));
            }
        }
    }
}
