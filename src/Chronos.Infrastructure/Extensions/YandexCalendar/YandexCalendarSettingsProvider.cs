using Chronos.Application.Extensions;
using Chronos.Application.Extensions.YandexCalendar;
using System.Security.Cryptography;
using Chronos.Application.Extensions.YandexCalendar.Dto;
using Chronos.Application.Security;
using Chronos.Domain.Entities.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Extensions.YandexCalendar
{
    public class YandexCalendarSettingsProvider : IYandexCalendarSettingsProvider
    {
        private readonly IUserExtensionRepository _repository;
        private readonly ISecretProtector _protector;

        public YandexCalendarSettingsProvider(IUserExtensionRepository repository, ISecretProtector protector)
        {
            _repository = repository;
            _protector = protector;
        }

        public async Task<YandexCalendarSettingsDto?> GetSettingsAsync(string username, CancellationToken ct = default)
        {
            var entity = await _repository.GetAsync(username, ExtensionType.YandexCalendar, ct);
            if (entity is null || string.IsNullOrEmpty(entity.Settings))
                return null;

            var stored = JsonSerializer.Deserialize<StoredSettings>(entity.Settings);
            if (stored is null) return null;

            string plainPassword;
            try
            {
                plainPassword = _protector.Unprotect(stored.AppPasswordEncrypted);
            }
            catch (CryptographicException)
            {
                // Stored password was encrypted with a different key (e.g. after app restart or migration).
                // Treat as missing so the user can re-enter credentials.
                return null;
            }

            var mappings = stored.IssueMappings?
                .Select(m => new YandexCalendarIssueMapping(m.Phrase, m.IssueKey))
                .ToList()
                ?? new List<YandexCalendarIssueMapping>();

            return new YandexCalendarSettingsDto(
                stored.Login,
                plainPassword,
                stored.ExcludedPhrases ?? new List<string>(),
                mappings);
        }

        private record StoredMapping(string Phrase, string IssueKey);

        private record StoredSettings(
            string Login,
            string AppPasswordEncrypted,
            List<string>? ExcludedPhrases = null,
            List<StoredMapping>? IssueMappings = null);
    }
}
