using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Releases
{
    /// <summary>
    /// Provides application releases for the Releases section.
    /// </summary>
    public interface IReleaseService
    {
        /// <summary>
        /// Returns published releases (newest first) together with a failure flag and a direct
        /// GitHub link, so the UI can offer the user a fallback when the live fetch fails.
        /// </summary>
        Task<ReleasesResult> GetReleasesAsync(CancellationToken cancellationToken = default);
    }
}
