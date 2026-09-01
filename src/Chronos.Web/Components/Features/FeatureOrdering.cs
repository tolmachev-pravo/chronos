using System;
using System.Collections.Generic;
using System.Linq;

namespace Chronos.Web.Components.Features
{
    /// <summary>
    /// The order every place that lists features reads them in — the catalog page and the
    /// worklog widget alike, so a reader meets the same articles in the same sequence in both.
    /// </summary>
    public static class FeatureOrdering
    {
        /// <summary>
        /// Highlights that have not expired yet (see
        /// <see cref="FeatureMetadata.HighlightDurationDays"/>) first, then everything else by
        /// date descending. The first item is what <c>/features</c> shows as its hero, so an
        /// article is only allowed to hold that spot for as long as its highlight lasts.
        /// </summary>
        public static IReadOnlyList<FeatureSummary> ForCatalog(
            IEnumerable<FeatureSummary> features,
            DateOnly today) =>
            features
                .OrderByDescending(feature => feature.Metadata.IsHighlightActiveOn(today))
                .ThenByDescending(feature => feature.Metadata.Date)
                .ToList();
    }
}
