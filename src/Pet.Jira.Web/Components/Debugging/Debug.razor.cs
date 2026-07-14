using Microsoft.AspNetCore.Components;
using Pet.Jira.Application.Tracing;
using System.Text;

namespace Pet.Jira.Web.Components.Debugging
{
    public partial class Debug : ComponentBase
    {
        [Inject] private IPerformanceStatsCollector StatsCollector { get; set; } = default!;

        public string Body => GetBody();

        private string GetBody()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Measure.Headers);
            stringBuilder.AppendLine(Measure.HeaderDelimeter);
            foreach (var measure in StatsCollector.Measures)
            {
                stringBuilder.AppendLine(measure.ToString());
            }

            return stringBuilder.ToString();
        }
    }
}
