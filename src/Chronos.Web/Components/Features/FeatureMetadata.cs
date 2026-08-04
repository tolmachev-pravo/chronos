using System;
using System.Collections.Generic;

namespace Chronos.Web.Components.Features
{
    /// <summary>
    /// Structured metadata for a feature, deserialized from its <c>metadata.json</c> file.
    /// </summary>
    public sealed class FeatureMetadata
    {
        /// <summary>
        /// Kebab-case identifier; equals the feature folder name.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable feature title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Author of the feature.
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Release date; features are sorted by this descending (newest first).
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// Optional tags.
        /// </summary>
        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// When <c>true</c>, the feature is pinned to the top of the section.
        /// </summary>
        public bool IsHighlighted { get; set; }
    }
}
