using NUnit.Framework;
using Chronos.Application.Extensions.YandexCalendar;
using Chronos.Application.Extensions.YandexCalendar.Dto;
using System.Collections.Generic;

namespace Chronos.UnitTests.Application.Extensions.YandexCalendar
{
    [TestFixture]
    public class YandexCalendarIssueMappingMatcherTests
    {
        [TestCase("Core Daily Sync", "Core Daily Sync", true, TestName = "Exact phrase matches the same title")]
        [TestCase("core daily sync", "Core Daily Sync", true, TestName = "Exact phrase is case insensitive")]
        [TestCase("Core Daily", "Core Daily Sync", false, TestName = "Exact phrase does not match by prefix")]
        [TestCase("Core Daily*", "Core Daily Sync", true, TestName = "Trailing asterisk matches the suffix")]
        [TestCase("Core Daily*", "Core Daily", true, TestName = "Trailing asterisk matches an empty tail")]
        [TestCase("*Sync", "Core Daily Sync", true, TestName = "Leading asterisk matches the prefix")]
        [TestCase("*Daily*", "Core Daily Sync", true, TestName = "Surrounding asterisks match a substring")]
        [TestCase("Core*Sync", "Core Daily Sync", true, TestName = "Asterisk in the middle matches")]
        [TestCase("Core*Sync", "Core Daily Groom", false, TestName = "Asterisk in the middle requires the tail")]
        [TestCase("Sprint ?", "Sprint 7", true, TestName = "Question mark matches a single char")]
        [TestCase("Sprint ?", "Sprint 12", false, TestName = "Question mark does not match two chars")]
        [TestCase("Sprint ?", "Sprint ", false, TestName = "Question mark requires a char")]
        [TestCase("Груминг*", "Груминг команды", true, TestName = "Mask works for cyrillic titles")]
        [TestCase("[CASEM]*", "[CASEM] Груминг", true, TestName = "Special regex chars are treated literally")]
        [TestCase("Core Daily**", "Core Daily Sync", true, TestName = "Repeated asterisks collapse into one")]
        [TestCase("  Core Daily*  ", "Core Daily Sync", true, TestName = "Phrase is trimmed before matching")]
        [TestCase("", "Core Daily Sync", false, TestName = "Empty phrase never matches")]
        [TestCase("   ", "Core Daily Sync", false, TestName = "Blank phrase never matches")]
        public void Matches_HandlesExactPhrasesAndMasks(string phrase, string summary, bool expected)
        {
            Assert.That(YandexCalendarIssueMappingMatcher.Matches(phrase, summary), Is.EqualTo(expected));
        }

        [Test]
        public void Matches_ReturnsFalse_ForNullSummary()
        {
            Assert.That(YandexCalendarIssueMappingMatcher.Matches("Core*", null), Is.False);
        }

        [TestCase("Core Daily Sync", false)]
        [TestCase("Core Daily*", true)]
        [TestCase("Sprint ?", true)]
        [TestCase("", false)]
        public void IsMask_DetectsWildcards(string phrase, bool expected)
        {
            Assert.That(YandexCalendarIssueMappingMatcher.IsMask(phrase), Is.EqualTo(expected));
        }

        [TestCase("  Core Daily*  ", "Core Daily*")]
        [TestCase("Core**Daily***", "Core*Daily*")]
        [TestCase("   ", "")]
        [TestCase(null, "")]
        public void Normalize_TrimsAndCollapsesAsterisks(string? phrase, string expected)
        {
            Assert.That(YandexCalendarIssueMappingMatcher.Normalize(phrase), Is.EqualTo(expected));
        }

        [Test]
        public void FindBestMatch_PrefersExactPhraseOverMask()
        {
            var mappings = new List<YandexCalendarIssueMapping>
            {
                new("Core Daily*", "CASEM-1"),
                new("Core Daily Sync", "CASEM-2"),
            };

            var match = YandexCalendarIssueMappingMatcher.FindBestMatch(mappings, "Core Daily Sync");

            Assert.That(match?.IssueKey, Is.EqualTo("CASEM-2"));
        }

        [Test]
        public void FindBestMatch_PrefersMoreSpecificMask()
        {
            var mappings = new List<YandexCalendarIssueMapping>
            {
                new("*", "CASEM-1"),
                new("Core*", "CASEM-2"),
                new("Core Daily*", "CASEM-3"),
            };

            var match = YandexCalendarIssueMappingMatcher.FindBestMatch(mappings, "Core Daily Sync");

            Assert.That(match?.IssueKey, Is.EqualTo("CASEM-3"));
        }

        [Test]
        public void FindBestMatch_PrefersFewerWildcards_WhenLiteralsAreEqual()
        {
            var mappings = new List<YandexCalendarIssueMapping>
            {
                new("*Core*Sync*", "CASEM-1"),
                new("Core*Sync", "CASEM-2"),
            };

            var match = YandexCalendarIssueMappingMatcher.FindBestMatch(mappings, "Core Daily Sync");

            Assert.That(match?.IssueKey, Is.EqualTo("CASEM-2"));
        }

        [Test]
        public void FindBestMatch_KeepsTheFirstMapping_WhenSpecificityIsEqual()
        {
            var mappings = new List<YandexCalendarIssueMapping>
            {
                new("Core*Sync", "CASEM-1"),
                new("Core*Sync", "CASEM-2"),
            };

            var match = YandexCalendarIssueMappingMatcher.FindBestMatch(mappings, "Core Daily Sync");

            Assert.That(match?.IssueKey, Is.EqualTo("CASEM-1"));
        }

        [Test]
        public void FindBestMatch_ReturnsNull_WhenNothingMatches()
        {
            var mappings = new List<YandexCalendarIssueMapping>
            {
                new("Core Daily*", "CASEM-1"),
            };

            Assert.That(YandexCalendarIssueMappingMatcher.FindBestMatch(mappings, "Retro"), Is.Null);
        }

        [Test]
        public void FindBestMatch_ReturnsNull_WhenMappingsAreEmptyOrNull()
        {
            Assert.Multiple(() =>
            {
                Assert.That(YandexCalendarIssueMappingMatcher.FindBestMatch(new List<YandexCalendarIssueMapping>(), "Retro"), Is.Null);
                Assert.That(YandexCalendarIssueMappingMatcher.FindBestMatch(null, "Retro"), Is.Null);
            });
        }
    }
}
