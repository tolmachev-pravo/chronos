using Chronos.Web.Components.Features;

namespace Chronos.UnitTests.Web.Features
{
    public class FeatureOrderingTests
    {
        private static readonly DateOnly Today = new(2026, 9, 1);

        [Test]
        public void ForCatalog_HighlightWithinItsWindow_ComesFirst()
        {
            // Arrange
            var highlighted = Feature("highlighted", Today.AddDays(-10), isHighlighted: true);
            var newest = Feature("newest", Today);

            // Act
            var result = FeatureOrdering.ForCatalog(new[] { newest, highlighted }, Today);

            // Assert
            Assert.That(Ids(result), Is.EqualTo(new[] { "highlighted", "newest" }));
        }

        [Test]
        public void ForCatalog_ExpiredHighlight_FallsBackToDateOrder()
        {
            // Arrange
            var stale = Feature("stale", Today.AddDays(-FeatureMetadata.HighlightDurationDays - 1), isHighlighted: true);
            var newest = Feature("newest", Today.AddDays(-1));

            // Act
            var result = FeatureOrdering.ForCatalog(new[] { stale, newest }, Today);

            // Assert
            Assert.That(Ids(result), Is.EqualTo(new[] { "newest", "stale" }));
        }

        [Test]
        public void ForCatalog_HighlightOnItsLastDay_IsStillPinned()
        {
            // Arrange
            var expiring = Feature("expiring", Today.AddDays(-FeatureMetadata.HighlightDurationDays), isHighlighted: true);
            var newest = Feature("newest", Today);

            // Act
            var result = FeatureOrdering.ForCatalog(new[] { newest, expiring }, Today);

            // Assert
            Assert.That(Ids(result), Is.EqualTo(new[] { "expiring", "newest" }));
        }

        [Test]
        public void ForCatalog_SeveralActiveHighlights_KeepsTheNewestOfThemFirst()
        {
            // Arrange
            var older = Feature("older", Today.AddDays(-20), isHighlighted: true);
            var newer = Feature("newer", Today.AddDays(-5), isHighlighted: true);
            var plain = Feature("plain", Today);

            // Act
            var result = FeatureOrdering.ForCatalog(new[] { older, plain, newer }, Today);

            // Assert
            Assert.That(Ids(result), Is.EqualTo(new[] { "newer", "older", "plain" }));
        }

        private static FeatureSummary Feature(string id, DateOnly date, bool isHighlighted = false) =>
            new(new FeatureMetadata { Id = id, Date = date, IsHighlighted = isHighlighted }, string.Empty);

        private static string[] Ids(IReadOnlyList<FeatureSummary> features) =>
            features.Select(feature => feature.Metadata.Id).ToArray();
    }
}
