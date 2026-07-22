using Pet.Jira.Application.Time;
using Pet.Jira.Domain.Models.Worklogs;
using Pet.Jira.Infrastructure.Jira.Dto;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Pet.Jira.Infrastructure.Jira
{
    public static class JiraExtensions
    {
        public static bool IsJiraKey(this string input)
        {
            var pattern = "^[A-Z][A-Z0-9]+-[0-9]+$";
			var regex = new Regex(pattern);
			var match = regex.Match(input);
            return match.Success;
		}

        public static IEnumerable<T> ConvertTo<T>(this IList<IssueChangeLogItemDto> issueChangeLogItems,
            string statusName,
            ITimeProvider timeProvider,
            TimeZoneInfo timeZoneInfo,
			WorklogSource worklogSource)
            where T : IWorklog, new()
        {
            var i = 0;
            while (i < issueChangeLogItems.Count)
            {
                var item = issueChangeLogItems[i];
                // 1. Первый элемент сразу выходит из прогресса. Значит это завершающий
                if (item.FromValue == statusName)
                {
                    yield return new T()
                    {
                        CompleteDate = timeProvider.ConvertToUserTimezone(item.ChangeLog.CreatedDate, timeZoneInfo),
                        StartDate = DateTime.MinValue,
                        Issue = item.ChangeLog.Issue.Adapt(),
                        Author = item.Author,
                        Source = worklogSource
					};
                }
                // 2. Это последний элемент и он не завершается
                else if (i == (issueChangeLogItems.Count - 1))
                {
                    yield return new T()
                    {
                        CompleteDate = DateTime.MaxValue,
                        StartDate = timeProvider.ConvertToUserTimezone(item.ChangeLog.CreatedDate, timeZoneInfo),
                        Issue = item.ChangeLog.Issue.Adapt(),
                        Author = item.Author,
                        Source = worklogSource
					};
                }
                // 3. Обычный случай когда после FromInProgress следует ToInProgress
                else
                {
                    yield return new T()
                    {
                        CompleteDate = timeProvider.ConvertToUserTimezone(issueChangeLogItems[i + 1].ChangeLog.CreatedDate, timeZoneInfo),
                        StartDate = timeProvider.ConvertToUserTimezone(item.ChangeLog.CreatedDate, timeZoneInfo),
                        Issue = item.ChangeLog.Issue.Adapt(),
                        Author = item.Author,
                        Source = worklogSource
					};
                }

                i += 2;
            }
        }

        public static IEnumerable<T> ConvertTo<T>(
            this List<IssueCommentDto> comments,
            ITimeProvider timeProvider,
            TimeZoneInfo timeZoneInfo,
            WorklogSource source,
            TimeSpan time)
                where T : IWorklog, new()
        {
            foreach (var comment in comments)
            {
                var createdDate = timeProvider.ConvertToUserTimezone(comment.CreatedDate, timeZoneInfo);
                yield return new T()
                {
                    CompleteDate = createdDate,
                    StartDate = createdDate.Add(-time),
                    Issue = comment.Issue.Adapt(),
                    Author = comment.Author,
                    Source = source
                };
            }
        }
    }
}
