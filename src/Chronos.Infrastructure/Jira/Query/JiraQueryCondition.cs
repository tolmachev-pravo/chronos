namespace Chronos.Infrastructure.Jira.Query
{
    public class JiraQueryCondition
    {
        public string Left { get; set; }
        public string Right { get; set; }
        public JiraQueryComparisonType ComparisonType { get; set; }

        public override string ToString() => $"{Left} {JiraQueryConstants.ComparisonTypes[ComparisonType]} {Right}";
    }
}
