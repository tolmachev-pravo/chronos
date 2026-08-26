using Chronos.Application.Extensions.YandexCalendar.Dto;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Chronos.Application.Extensions.YandexCalendar
{
    /// <summary>
    /// Сопоставляет название события календаря с фразой маппинга на задачу Jira.
    /// Фраза без подстановочных символов сравнивается точно (поведение до issue #281),
    /// фраза с '*' (любое число символов) или '?' (ровно один символ) трактуется как маска.
    /// Сравнение регистронезависимое.
    /// </summary>
    public static class YandexCalendarIssueMappingMatcher
    {
        private static readonly ConcurrentDictionary<string, Regex> MaskCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Содержит ли фраза подстановочные символы.</summary>
        public static bool IsMask(string? phrase) =>
            !string.IsNullOrEmpty(phrase) && (phrase.Contains('*') || phrase.Contains('?'));

        /// <summary>
        /// Приводит фразу к каноническому виду: убирает крайние пробелы и схлопывает
        /// подряд идущие '*' — 'Daily**' и 'Daily*' задают одну и ту же маску.
        /// </summary>
        public static string Normalize(string? phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase))
                return string.Empty;

            var trimmed = phrase.Trim();
            var builder = new StringBuilder(trimmed.Length);
            foreach (var ch in trimmed)
            {
                if (ch == '*' && builder.Length > 0 && builder[^1] == '*')
                    continue;
                builder.Append(ch);
            }

            return builder.ToString();
        }

        /// <summary>Подходит ли название события под фразу маппинга.</summary>
        public static bool Matches(string? phrase, string? summary)
        {
            var pattern = Normalize(phrase);
            if (pattern.Length == 0)
                return false;

            summary ??= string.Empty;

            return IsMask(pattern)
                ? MaskCache.GetOrAdd(pattern, BuildRegex).IsMatch(summary)
                : string.Equals(pattern, summary, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Ищет самый специфичный маппинг для названия события: сначала точные совпадения,
        /// затем маски — от более специфичной (больше значащих символов, меньше подстановок)
        /// к более общей. При равной специфичности выигрывает тот, что выше в списке.
        /// </summary>
        public static YandexCalendarIssueMapping? FindBestMatch(
            IEnumerable<YandexCalendarIssueMapping>? mappings,
            string? summary)
        {
            if (mappings is null)
                return null;

            YandexCalendarIssueMapping? best = null;
            var bestRank = default(MatchRank);

            foreach (var mapping in mappings)
            {
                if (!Matches(mapping.Phrase, summary))
                    continue;

                var rank = RankOf(Normalize(mapping.Phrase));
                if (best is null || rank.IsMoreSpecificThan(bestRank))
                {
                    best = mapping;
                    bestRank = rank;
                }
            }

            return best;
        }

        private static MatchRank RankOf(string pattern)
        {
            var wildcards = 0;
            var literals = 0;
            foreach (var ch in pattern)
            {
                if (ch is '*' or '?')
                    wildcards++;
                else
                    literals++;
            }

            return new MatchRank(wildcards == 0, literals, wildcards);
        }

        private static Regex BuildRegex(string pattern)
        {
            var builder = new StringBuilder(pattern.Length * 2 + 2).Append('^');
            var literal = new StringBuilder();

            void FlushLiteral()
            {
                if (literal.Length == 0)
                    return;
                builder.Append(Regex.Escape(literal.ToString()));
                literal.Clear();
            }

            foreach (var ch in pattern)
            {
                switch (ch)
                {
                    case '*':
                        FlushLiteral();
                        builder.Append(".*");
                        break;
                    case '?':
                        FlushLiteral();
                        builder.Append('.');
                        break;
                    default:
                        literal.Append(ch);
                        break;
                }
            }

            FlushLiteral();
            builder.Append('$');

            return new Regex(
                builder.ToString(),
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
        }

        private readonly record struct MatchRank(bool IsExact, int LiteralLength, int WildcardCount)
        {
            public bool IsMoreSpecificThan(MatchRank other)
            {
                if (IsExact != other.IsExact)
                    return IsExact;
                if (LiteralLength != other.LiteralLength)
                    return LiteralLength > other.LiteralLength;
                return WildcardCount < other.WildcardCount;
            }
        }
    }
}
