using System;

namespace Chronos.Web.Mcp.Contracts
{
    /// <summary>
    /// What went into Jira, answered back so the client can show the user what it did.
    /// </summary>
    public record AddedWorklogView(
        string IssueKey,
        string Summary,
        DateTime StartedAt,
        int Minutes,
        string Comment);
}
