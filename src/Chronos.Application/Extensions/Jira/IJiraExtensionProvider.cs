using Chronos.Application.Extensions.Jira.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Extensions.Jira
{
    /// <summary>
    /// The user's Jira extension state, readable outside the MediatR pipeline — the
    /// event providers need it in their sequential prepare phase. See issue #299.
    /// </summary>
    public interface IJiraExtensionProvider
    {
        Task<JiraExtensionDto> GetAsync(string username, CancellationToken cancellationToken = default);
    }
}
