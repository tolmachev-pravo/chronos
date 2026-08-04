namespace Chronos.Web.Components.Features
{
    /// <summary>
    /// Lightweight feature representation used on cards and in the worklog widget.
    /// </summary>
    public sealed class FeatureSummary
    {
        public FeatureSummary(FeatureMetadata metadata, string previewMarkdown)
        {
            Metadata = metadata;
            PreviewMarkdown = previewMarkdown;
        }

        public FeatureMetadata Metadata { get; }

        /// <summary>
        /// Rendered content of <c>preview.md</c>.
        /// </summary>
        public string PreviewMarkdown { get; }
    }
}
