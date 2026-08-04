namespace Chronos.Web.Components.Features
{
    /// <summary>
    /// Full feature representation used in the detail view.
    /// </summary>
    public sealed class FeatureDetail
    {
        public FeatureDetail(FeatureMetadata metadata, string indexMarkdown)
        {
            Metadata = metadata;
            IndexMarkdown = indexMarkdown;
        }

        public FeatureMetadata Metadata { get; }

        /// <summary>
        /// Rendered content of <c>index.md</c>.
        /// </summary>
        public string IndexMarkdown { get; }
    }
}
