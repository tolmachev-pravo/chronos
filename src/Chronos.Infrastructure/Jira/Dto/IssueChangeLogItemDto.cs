namespace Chronos.Infrastructure.Jira.Dto
{
    public class IssueChangeLogItemDto
    {
        public string FromId { get; set; }
        public string ToId { get; set; }
        public string FromValue { get; set; }
        public string ToValue { get; set; }
        public IssueChangeLogDto ChangeLog { get; set; }
        public string Author { get; set; }
    }
}
