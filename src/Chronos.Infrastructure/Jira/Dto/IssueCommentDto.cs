using System;

namespace Chronos.Infrastructure.Jira.Dto
{
    public class IssueCommentDto
    {
        public DateTime CreatedDate { get; set; }
        public IssueDto Issue { get; set; }
        public string Author { get; set; }

        public static IssueCommentDto Create(
            Atlassian.Jira.Comment comment,
            IssueDto issue)
        {
            return new IssueCommentDto
            {
                CreatedDate = comment.CreatedDate.Value,
                Issue = issue,
                Author = comment.Author
            };
        }
    }
}
