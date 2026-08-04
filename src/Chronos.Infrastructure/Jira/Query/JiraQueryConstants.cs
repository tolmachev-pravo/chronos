using System.Collections.Generic;

namespace Chronos.Infrastructure.Jira.Query
{
    public class JiraQueryConstants
    {
        public static Dictionary<JiraQueryMacros, string> Macroses =
            new Dictionary<JiraQueryMacros, string>()
            {
                { JiraQueryMacros.CurrentUser, "currentUser()" }
            };

        public static Dictionary<JiraQueryComparisonType, string> ComparisonTypes =
            new Dictionary<JiraQueryComparisonType, string>
            {
                { JiraQueryComparisonType.Equal, "=" },
                { JiraQueryComparisonType.Greater, ">" },
                { JiraQueryComparisonType.GreaterOrEqual, ">=" },
                { JiraQueryComparisonType.Less, "<" },
                { JiraQueryComparisonType.LessOrEqual, "<=" },
                { JiraQueryComparisonType.NotEqual, "!=" },
            };

        public static Dictionary<JiraQueryOrderType, string> OrderTypes =
            new Dictionary<JiraQueryOrderType, string>
            {
                { JiraQueryOrderType.Asc, "ASC" },
                { JiraQueryOrderType.Desc, "DESC" }
            };

        public static class Date
        {
            public const string DefaultFormat = "yyyy/MM/dd";
        }
    }
}
