using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chronos.Web.Components.Features
{
    /// <summary>
    /// Structured metadata for a feature, deserialized from its <c>metadata.json</c> file.
    /// </summary>
    public sealed class FeatureMetadata
    {
        /// <summary>
        /// How long a highlight keeps a feature at the top of the catalog, counted from
        /// <see cref="Date"/>. Importance has a shelf life: while it had none, a pinned
        /// article stayed first forever and the section stopped showing what is new.
        /// </summary>
        public const int HighlightDurationDays = 30;

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
        /// When <c>true</c>, the feature is pinned to the top of the section for
        /// <see cref="HighlightDurationDays"/> days after <see cref="Date"/>.
        /// </summary>
        public bool IsHighlighted { get; set; }

        /// <summary>
        /// Whether the highlight still counts today. Once it expires the feature goes back
        /// into the plain chronological order and drops the "recommended" badge.
        /// </summary>
        [JsonIgnore]
        public bool IsHighlightActive => IsHighlightActiveOn(DateOnly.FromDateTime(DateTime.Today));

        /// <summary>
        /// Whether the highlight still counts on <paramref name="today"/>.
        /// </summary>
        public bool IsHighlightActiveOn(DateOnly today) =>
            IsHighlighted && today <= Date.AddDays(HighlightDurationDays);
    }
}
