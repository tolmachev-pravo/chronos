using MediatR;
using Chronos.Application.Extensions.YandexCalendar.Dto;
using Chronos.Application.Security;
using Chronos.Domain.Entities.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Extensions.YandexCalendar.Commands
{
    public class UpsertYandexCalendarExtension
    {
        public record Command(
            string Username,
            YandexCalendarSettingsDto Settings,
            bool IsEnabled) : IRequest;

        public class Handler : IRequestHandler<Command>
        {
            private readonly IUserExtensionRepository _repository;
            private readonly ISecretProtector _protector;

            public Handler(IUserExtensionRepository repository, ISecretProtector protector)
            {
                _repository = repository;
                _protector = protector;
            }

            public async Task Handle(Command request, CancellationToken ct)
            {
                var stored = new StoredSettings(
                    request.Settings.Login,
                    _protector.Protect(request.Settings.AppPassword),
                    new List<string>(request.Settings.ExcludedPhrases),
                    request.Settings.IssueMappings
                        .Select(m => new StoredMapping(m.Phrase, m.IssueKey))
                        .ToList());

                var existing = await _repository.GetAsync(request.Username, ExtensionType.YandexCalendar, ct);

                var entity = existing ?? new UserExtension
                {
                    Username = request.Username,
                    Type = ExtensionType.YandexCalendar,
                    CreatedAt = DateTime.UtcNow
                };

                entity.IsEnabled = request.IsEnabled;
                entity.Settings = JsonSerializer.Serialize(stored);
                entity.UpdatedAt = DateTime.UtcNow;

                await _repository.UpsertAsync(entity, ct);
            }

            private record StoredMapping(string Phrase, string IssueKey);

            private record StoredSettings(
                string Login,
                string AppPasswordEncrypted,
                List<string>? ExcludedPhrases = null,
                List<StoredMapping>? IssueMappings = null);
        }
    }
}
