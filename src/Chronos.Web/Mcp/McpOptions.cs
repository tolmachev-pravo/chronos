namespace Chronos.Web.Mcp
{
    /// <summary>
    /// Settings of the MCP server (issue #298). Off by default: the endpoint appears only
    /// where it is turned on deliberately.
    /// </summary>
    public class McpOptions
    {
        public const string SectionName = "Mcp";

        public bool Enabled { get; set; }

        /// <summary>
        /// Where the server listens. A client is configured with this path, so it changes
        /// only together with every client already pointed at it.
        /// </summary>
        public string Path { get; set; } = "/mcp";
    }
}
