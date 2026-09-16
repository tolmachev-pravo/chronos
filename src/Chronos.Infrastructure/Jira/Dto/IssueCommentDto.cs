using System;

namespace Chronos.Infrastructure.Jira.Dto
{
    public class IssueCommentDto
    {
        /// <summary>
        /// Jira's own id for the comment. What a link to the comment is built from.
        /// See issue #156.
        /// </summary>
        public string Id { get; set; }

        public DateTime CreatedDate { get; set; }
        public IssueDto Issue { get; set; }

        /// <summary>
        /// The username the comment is filtered by — the provider matches it against the
        /// user's own profile, so it has to keep meaning the username.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// The same author, spelled the way a person is named in the UI. Null when Jira
        /// answered without the author's profile. See issue #156.
        /// </summary>
        public string AuthorDisplayName { get; set; }

        /// <summary>
        /// The comment itself, as Jira stores it: wiki markup, not rendered HTML.
        /// See issue #156.
        /// </summary>
        public string Body { get; set; }

        public static IssueCommentDto Create(
            Atlassian.Jira.Comment comment,
            IssueDto issue)
        {
            return new IssueCommentDto
            {
                Id = comment.Id,
                CreatedDate = comment.CreatedDate.Value,
                Issue = issue,
                Author = comment.Author,
                AuthorDisplayName = comment.AuthorUser?.DisplayName,
                Body = comment.Body
            };
        }
    }
}
