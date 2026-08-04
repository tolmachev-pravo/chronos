using System;
using System.Globalization;

namespace Chronos.Infrastructure.Jira.Query
{
    public class JiraQueryWasCondition
    {
        public string Field { get; set; }
        public string Value { get; set; }
        public DateTime DuringFrom { get; set; }
        public DateTime DuringTo { get; set; }

        public override string ToString()
        {
            var format = JiraQueryConstants.Date.DefaultFormat;
            var from = DuringFrom.ToString(format, CultureInfo.InvariantCulture);
            var to = DuringTo.ToString(format, CultureInfo.InvariantCulture);
            return $"{Field} WAS '{Value}' DURING ('{from}', '{to}')";
        }
    }
}
