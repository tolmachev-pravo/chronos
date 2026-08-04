using System;

namespace Chronos.Infrastructure.Jira.Dto
{
    public class IssueChangeLogDto
    {
        public DateTime CreatedDate { get; set; }
        public IssueDto Issue { get; set; }

        public static IssueChangeLogDto Create(
            Atlassian.Jira.IssueChangeLog changeLog,
            IssueDto issue)
        {
            return new IssueChangeLogDto
            {
                CreatedDate = changeLog.CreatedDate,
                Issue = issue
            };
        }
    }
}
