using System;
using System.Collections.Generic;

namespace Pet.Jira.Web.Components.Releases
{
    /// <summary>
    /// Outcome of a releases lookup: the releases to show (possibly cached), whether the live
    /// fetch failed, and a direct link the user can open to view releases on GitHub.
    /// </summary>
    public sealed class ReleasesResult
    {
        public IReadOnlyList<ReleaseSummary> Releases { get; init; } = Array.Empty<ReleaseSummary>();

        /// <summary>
        /// <c>true</c> when the live GitHub request failed (a cached copy may still be shown).
        /// </summary>
        public bool LoadFailed { get; init; }

        /// <summary>
        /// Public GitHub releases page for the configured repository, offered to the user as a fallback.
        /// </summary>
        public string ReleasesUrl { get; init; }
    }
}
