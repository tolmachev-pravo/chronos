using System;
using Pet.Jira.Domain.Models.Issues;

namespace Pet.Jira.Infrastructure.Jira
{
    public static class JiraConstants
    {
        public const int DefaultMaxIssuesPerRequest = int.MaxValue;

        public static class Status
        {
            public const string FieldName = "status";
            public static IssueStatus InProgress => new() { Id = "3", Name = "In Progress" };
            public static IssueStatus InTesting => new() { Id = "10116", Name = "In Testing" };
            public static IssueStatus Default => InProgress;
        }
    }
}
