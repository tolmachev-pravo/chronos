using Atlassian.Jira;

namespace Chronos.Infrastructure.Jira.Dto
{
    public class IssueStatusDto
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public static IssueStatusDto Create(IssueStatus issueStatus)
        {
            return new IssueStatusDto
            {
                Id = issueStatus.Id,
                Name = issueStatus.Name
            };
        }
    }
}
