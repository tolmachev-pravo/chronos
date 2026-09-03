using Chronos.Application.Extensions;
using Chronos.Application.Extensions.Jira;
using Chronos.Application.Extensions.Jira.Dto;
using Chronos.Domain.Entities.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Extensions.Jira
{
    public class JiraExtensionProvider : IJiraExtensionProvider
    {
        private readonly IUserExtensionRepository _repository;

        public JiraExtensionProvider(IUserExtensionRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// A user without a stored extension is treated as connected with the default
        /// settings, so worklog search keeps working before the extension is seeded.
        /// </summary>
        public async Task<JiraExtensionDto> GetAsync(string username, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetAsync(username, ExtensionType.Jira, cancellationToken);
            if (entity is null)
                return new JiraExtensionDto(true, JiraExtensionSettingsDto.Default);

            return new JiraExtensionDto(
                entity.IsEnabled,
                JiraExtensionSettingsSerializer.Deserialize(entity.Settings));
        }
    }
}
