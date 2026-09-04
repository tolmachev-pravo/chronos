using Microsoft.Extensions.DependencyInjection;
using Chronos.Web.Common;
using Chronos.Web.Mcp.Tools;

namespace Chronos.Web.Mcp
{
    public static class McpServiceCollectionExtensions
    {
        /// <summary>
        /// The MCP server itself (issue #298): streamable HTTP transport and the tools of
        /// <see cref="WorklogTools"/>. The transport is stateless by default, which is what
        /// this server wants — every call is authenticated by the token it carries, so
        /// there is no session to keep credentials in.
        /// </summary>
        public static IServiceCollection AddChronosMcpServer(this IServiceCollection services)
        {
            services.AddTransient<WorklogTools>();
            services
                .AddMcpServer(options =>
                {
                    options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
                    {
                        Name = "chronos",
                        Title = "Chronos",
                        // The same version the site shows, stamped from the git tag.
                        Version = AppVersion.Display
                    };
                    options.ServerInstructions =
                        "Chronos fills in Jira worklogs. It reads the user's own activity — issues " +
                        "they worked on, issues they tested, issues they commented, meetings from " +
                        "their calendar — and suggests how much time to log where. Read a period " +
                        "with get_worklog_collection before logging anything: a suggestion already " +
                        "covered by a worklog in Jira is not offered again, and the day tells how " +
                        "much of it is still open. Logging time with add_worklog is immediate and " +
                        "cannot be undone from here, so confirm it with the user first.";
                })
                .WithHttpTransport()
                .WithTools<WorklogTools>();

            return services;
        }
    }
}
