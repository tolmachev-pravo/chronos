using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Web.Components.Releases
{
    /// <summary>
    /// Provides application releases for the Releases section.
    /// </summary>
    public interface IReleaseService
    {
        /// <summary>
        /// Returns published releases ordered from newest to oldest.
        /// </summary>
        Task<IReadOnlyList<ReleaseSummary>> GetReleasesAsync(CancellationToken cancellationToken = default);
    }
}
