namespace Chronos.Infrastructure.Jira.Query
{
    public class JiraQueryOrder
    {
        public string Parameter { get; set; }
        public JiraQueryOrderType OrderType { get; set; }

        public override string ToString() => $"{Parameter} {JiraQueryConstants.OrderTypes[OrderType]}";
    }
}
