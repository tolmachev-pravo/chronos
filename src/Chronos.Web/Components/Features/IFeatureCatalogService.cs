using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Features
{
    /// <summary>
    /// Discovers and reads feature documentation stored under
    /// <c>wwwroot/documents/features/{id}/</c>.
    /// </summary>
    public interface IFeatureCatalogService
    {
        /// <summary>
        /// Returns all features in catalog order: features whose highlight is still active
        /// first, then by date descending. Returns an empty list when no features exist.
        /// </summary>
        Task<IReadOnlyList<FeatureSummary>> GetFeaturesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the full detail for a single feature, or <c>null</c> when it does not exist.
        /// </summary>
        Task<FeatureDetail> GetFeatureAsync(string id, CancellationToken cancellationToken = default);
    }
}
