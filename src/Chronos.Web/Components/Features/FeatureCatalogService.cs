using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Chronos.Web.Components.Markdown;

namespace Chronos.Web.Components.Features
{
    /// <inheritdoc />
    public sealed class FeatureCatalogService : IFeatureCatalogService
    {
        private const string FeaturesRootPath = "wwwroot/documents/features";
        private const string MetadataFileName = "metadata.json";
        private const string PreviewFileName = "preview.md";
        private const string IndexFileName = "index.md";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Guards against path traversal: feature ids are kebab-case slugs only.
        private static readonly Regex ValidIdRegex = new("^[a-z0-9-]+$", RegexOptions.Compiled);

        private readonly IMarkdownService _markdownService;

        public FeatureCatalogService(IMarkdownService markdownService)
        {
            _markdownService = markdownService;
        }

        public async Task<IReadOnlyList<FeatureSummary>> GetFeaturesAsync(CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(FeaturesRootPath))
            {
                return Array.Empty<FeatureSummary>();
            }

            var summaries = new List<FeatureSummary>();
            foreach (var directory in Directory.GetDirectories(FeaturesRootPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var metadata = await ReadMetadataAsync(directory, cancellationToken);
                if (metadata is null)
                {
                    continue;
                }

                var preview = await ReadMarkdownAsync(Path.Combine(directory, PreviewFileName));
                summaries.Add(new FeatureSummary(metadata, preview));
            }

            return summaries
                .OrderByDescending(summary => summary.Metadata.IsHighlighted)
                .ThenByDescending(summary => summary.Metadata.Date)
                .ToList();
        }

        public async Task<FeatureDetail> GetFeatureAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id) || !ValidIdRegex.IsMatch(id))
            {
                return null;
            }

            var directory = Path.Combine(FeaturesRootPath, id);
            var metadata = await ReadMetadataAsync(directory, cancellationToken);
            if (metadata is null)
            {
                return null;
            }

            var index = await ReadMarkdownAsync(Path.Combine(directory, IndexFileName));
            return new FeatureDetail(metadata, index);
        }

        private static async Task<FeatureMetadata> ReadMetadataAsync(string directory, CancellationToken cancellationToken)
        {
            var path = Path.Combine(directory, MetadataFileName);
            if (!File.Exists(path))
            {
                return null;
            }

            await using var stream = File.OpenRead(path);
            var metadata = await JsonSerializer.DeserializeAsync<FeatureMetadata>(stream, JsonOptions, cancellationToken);
            if (metadata is null)
            {
                return null;
            }

            // The folder name is the source of truth for the id.
            metadata.Id = new DirectoryInfo(directory).Name;
            return metadata;
        }

        private async Task<string> ReadMarkdownAsync(string path)
        {
            return File.Exists(path)
                ? await _markdownService.DownloadMarkdownAsync(path)
                : string.Empty;
        }
    }
}
