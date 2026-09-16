namespace Chronos.Infrastructure.Jira
{
    public interface IJiraLinkGenerator
    {
        string Generate(string issueKey);

        /// <summary>
        /// A link that opens the issue with one comment in view. Lives here rather than in
        /// the comment source because logging a review comment wants the same link.
        /// See issue #156.
        /// </summary>
        string GenerateComment(string issueKey, string commentId);
    }
}
