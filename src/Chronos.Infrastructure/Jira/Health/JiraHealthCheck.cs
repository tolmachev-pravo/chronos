using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Jira.Health
{
    public class JiraHealthCheck : IHealthCheck
    {
        private readonly IJiraConfiguration _jiraConfiguration;
        public static string Name = "jira_access";

        public JiraHealthCheck(
            IOptions<JiraConfiguration> jiraConfiguration)
        {
            _jiraConfiguration = jiraConfiguration.Value;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var statusCode = await PingAsync(cancellationToken);
                var healthy = statusCode == HttpStatusCode.OK;
                var data = new Dictionary<string, object>
                {
                    {"Url", _jiraConfiguration.Url},
                    {"StatusCode", statusCode}
                };
                var description = $"Status code: {statusCode}";

                return healthy
                    ? HealthCheckResult.Healthy(description, data)
                    : HealthCheckResult.Unhealthy(description, data: data);
            }
            catch (Exception ex)
            {
                var data = new Dictionary<string, object>
                {
                    {"Url", _jiraConfiguration.Url}
                };
                return HealthCheckResult.Unhealthy(exception: ex, data: data);
            }
        }

        public async Task<HttpStatusCode> PingAsync(CancellationToken cancellationToken = default)
        {
            HttpClient httpClient = new();
            var content = await httpClient.GetAsync(_jiraConfiguration.Url, cancellationToken);
            return content.StatusCode;
        }
    }
}
