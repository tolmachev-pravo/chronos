using Chronos.Domain.Models.Abstract;
using Chronos.Domain.Models.Issues;

namespace Chronos.Infrastructure.Jira.Dto
{
    public class IssueDto : IEntity<string>
    {
        public string Key { get; set; }
        public string Summary { get; set; }
        public string Link { get; set; }
        public string Identifier { get; set; }
        public string Name => $"{Key}. {Summary}";

        public Issue Adapt()
        {
            return new Issue
            {
                Key = Key,
                Link = Link,
                Summary = Summary,
                Identifier = Identifier
            };
        }

        public static IssueDto Create(Atlassian.Jira.Issue issue, IJiraLinkGenerator linkGenerator = null)
        {
            return new IssueDto
            {
                Key = issue.Key.Value,
                Summary = issue.Summary,
                Link = linkGenerator?.Generate(issue.Key.Value),
                Identifier = issue.JiraIdentifier
            };
        }
    }
}
