using Microsoft.AspNetCore.Components;
using Pet.Jira.Application.Tracing;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Pet.Jira.Web.Components.Performance
{
    public partial class Performance : ComponentBase
    {
        [Inject] private IPerformanceStatsCollector StatsCollector { get; set; } = default!;

        private string Body => BuildMarkdownTable();

        private void Refresh() => StateHasChanged();

        private void Reset()
        {
            StatsCollector.Reset();
            StateHasChanged();
        }

        private string BuildMarkdownTable()
        {
            var measures = StatsCollector.Measures
                .OrderByDescending(measure => measure.Sum)
                .ToList();

            if (measures.Count == 0)
            {
                return "_Нет измерений. Выполните операцию (например, откройте ворклоги) и нажмите «Обновить»._";
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("| Category | Count | Sum, ms | Min, ms | Max, ms | Average, ms | Alloc avg | Alloc max | Alloc sum |");
            stringBuilder.AppendLine("|---|--:|--:|--:|--:|--:|--:|--:|--:|");
            foreach (var measure in measures)
            {
                stringBuilder.AppendLine(
                    $"| {measure.Category} | {measure.Count} | {Milliseconds(measure.Sum)} | {Milliseconds(measure.Min)} | {Milliseconds(measure.Max)} | {Milliseconds(measure.Average)} | {Bytes(measure.AllocatedAverage)} | {Bytes(measure.AllocatedMax)} | {Bytes(measure.AllocatedSum)} |");
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("_Alloc — объём аллокаций (произведённый «мусор» GC), а не удержанная память. Метрика процессная: корректна при отсутствии параллельных запросов._");

            return stringBuilder.ToString();
        }

        private static string Milliseconds(TimeSpan value) =>
            value.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture);

        private static string Bytes(long value)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = value;
            var unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return $"{size.ToString(unit == 0 ? "F0" : "F1", CultureInfo.InvariantCulture)} {units[unit]}";
        }
    }
}
