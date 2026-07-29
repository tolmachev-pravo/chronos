using MediatR;
using Pet.Jira.Application.Extensions.Jira.Dto;
using Pet.Jira.Domain.Entities.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Application.Extensions.Jira.Commands
{
    /// <summary>
    /// Connects the Jira extension for a user that has none yet — called right after login,
    /// so the settings are always visible in Extensions. Idempotent: an existing record is
    /// never touched.
    /// </summary>
    public class EnsureJiraExtension
    {
        /// <param name="HasLegacyFilter">
        /// True when the user already has a locally stored worklog filter, i.e. they used the
        /// app before the settings moved here. Such users keep their current behaviour:
        /// tester events stay on and the comment duration is carried over.
        /// </param>
        public record Command(
            string Username,
            bool HasLegacyFilter,
            TimeSpan? LegacyCommentWorklogTime) : IRequest;

        public class Handler : IRequestHandler<Command>
        {
            private readonly IUserExtensionRepository _repository;

            public Handler(IUserExtensionRepository repository)
            {
                _repository = repository;
            }

            public async Task Handle(Command request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(request.Username))
                    return;

                var existing = await _repository.GetAsync(request.Username, ExtensionType.Jira, cancellationToken);
                if (existing is not null)
                    return;

                var settings = BuildInitialSettings(request);

                await _repository.UpsertAsync(
                    new UserExtension
                    {
                        Username = request.Username,
                        Type = ExtensionType.Jira,
                        IsEnabled = true,
                        Settings = JiraExtensionSettingsSerializer.Serialize(settings),
                        CreatedAt = DateTime.UtcNow
                    },
                    cancellationToken);
            }

            private static JiraExtensionSettingsDto BuildInitialSettings(Command request)
            {
                if (!request.HasLegacyFilter)
                    return JiraExtensionSettingsDto.Default;

                var commentWorklogTime = request.LegacyCommentWorklogTime ?? TimeSpan.Zero;
                return new JiraExtensionSettingsDto(
                    AssigneeEventsEnabled: true,
                    CommentEventsEnabled: commentWorklogTime > TimeSpan.Zero,
                    CommentWorklogTime: commentWorklogTime,
                    TesterEventsEnabled: true);
            }
        }
    }
}
