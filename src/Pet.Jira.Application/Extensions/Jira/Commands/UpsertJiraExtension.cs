using MediatR;
using Pet.Jira.Application.Extensions.Jira.Dto;
using Pet.Jira.Domain.Entities.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Application.Extensions.Jira.Commands
{
    public class UpsertJiraExtension
    {
        public record Command(
            string Username,
            JiraExtensionSettingsDto Settings,
            bool IsEnabled) : IRequest;

        public class Handler : IRequestHandler<Command>
        {
            private readonly IUserExtensionRepository _repository;

            public Handler(IUserExtensionRepository repository)
            {
                _repository = repository;
            }

            public async Task Handle(Command request, CancellationToken cancellationToken)
            {
                var existing = await _repository.GetAsync(request.Username, ExtensionType.Jira, cancellationToken);

                var entity = existing ?? new UserExtension
                {
                    Username = request.Username,
                    Type = ExtensionType.Jira,
                    CreatedAt = DateTime.UtcNow
                };

                entity.IsEnabled = request.IsEnabled;
                entity.Settings = JiraExtensionSettingsSerializer.Serialize(request.Settings);
                entity.UpdatedAt = DateTime.UtcNow;

                await _repository.UpsertAsync(entity, cancellationToken);
            }
        }
    }
}
