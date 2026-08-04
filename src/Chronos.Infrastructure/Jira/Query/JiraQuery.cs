using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Chronos.Infrastructure.Jira.Query
{
    public class JiraQuery
    {
        private readonly List<string> _conditions;
        private readonly List<JiraQueryOrder> _orders;

        public JiraQuery()
        {
            _conditions = new List<string>();
            _orders = new List<JiraQueryOrder>();
        }

        public JiraQuery Where(string left, JiraQueryComparisonType comparisonType, object right)
        {
            _conditions.Add(new JiraQueryCondition
            {
                Left = left,
                ComparisonType = comparisonType,
                Right = GetStringValue(right)
            }.ToString());
            return this;
        }

        public JiraQuery WhereWas(string field, string value, DateTime duringFrom, DateTime duringTo)
        {
            _conditions.Add(new JiraQueryWasCondition
            {
                Field = field,
                Value = value,
                DuringFrom = duringFrom,
                DuringTo = duringTo
            }.ToString());
            return this;
        }

        /// <summary>
        /// Restricts the search to the issues commented by <paramref name="author"/>
        /// between <paramref name="after"/> and <paramref name="before"/> via the
        /// ScriptRunner "commented" issue function. See issue #259.
        /// </summary>
        public JiraQuery WhereCommented(JiraQueryMacros author, DateTime after, DateTime before)
        {
            _conditions.Add(new JiraQueryCommentedCondition
            {
                Author = author,
                After = after,
                Before = before
            }.ToString());
            return this;
        }

        public JiraQuery OrderBy(string parameter, JiraQueryOrderType orderType = JiraQueryOrderType.Asc)
        {
            _orders.Add(new JiraQueryOrder
            {
                Parameter = parameter,
                OrderType = orderType
            });
            return this;
        }

        private string GetStringValue(object value)
        {
            var stringValue = value.ToString();
            switch (value)
            {
                case JiraQueryMacros macrosValue:
                    return JiraQueryConstants.Macroses[macrosValue];
                case DateTime dateValue:
                    stringValue = dateValue.ToString(JiraQueryConstants.Date.DefaultFormat, CultureInfo.InvariantCulture);
                    break;
            }
            return $"'{stringValue}'";
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            if (_conditions.Any())
            {
                stringBuilder.Append(string.Join(" AND ", _conditions));
                stringBuilder.Append(' ');
            }
            if (_orders.Any())
            {
                stringBuilder.Append("ORDER BY ");
                stringBuilder.Append(string.Join(", ", _orders));
                stringBuilder.Append(' ');
            }

            return stringBuilder.ToString();
        }
    }
}
