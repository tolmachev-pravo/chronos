using Microsoft.Extensions.Options;
using Chronos.Application.Common.Extensions;

namespace Chronos.Infrastructure.Jira
{
    public class JiraLinkGenerator : IJiraLinkGenerator
    {
        private readonly IJiraConfiguration _config;

        public JiraLinkGenerator(IOptions<JiraConfiguration> jiraConfiguration)
        {
            _config = jiraConfiguration.Value;
        }

        public string Generate(string issueKey) => _config.Url.AppendUrl("browse", issueKey);

        /// <summary>
        /// The anchor alone scrolls the page; focusedCommentId is what makes Jira open the
        /// comment tab and expand the comment, so both are needed.
        /// </summary>
        public string GenerateComment(string issueKey, string commentId) =>
            $"{Generate(issueKey)}?focusedCommentId={commentId}"
            + $"&page={CommentTabPanel}#comment-{commentId}";

        private const string CommentTabPanel =
            "com.atlassian.jira.plugin.system.issuetabpanels:comment-tabpanel";
    }
}
