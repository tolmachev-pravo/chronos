using Chronos.Application.Time;
using Chronos.Domain.Models.Events;
using Chronos.Infrastructure.Jira.Dto;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Chronos.Infrastructure.Jira
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

        public static IEnumerable<UserEvent> ConvertTo(this IList<IssueChangeLogItemDto> issueChangeLogItems,
            string statusName,
            ITimeProvider timeProvider,
            TimeZoneInfo timeZoneInfo,
			EventSource eventSource)
        {
            var i = 0;
            while (i < issueChangeLogItems.Count)
            {
                var item = issueChangeLogItems[i];
                // 1. Первый элемент сразу выходит из прогресса. Значит это завершающий
                if (item.FromValue == statusName)
                {
                    yield return new UserEvent()
                    {
                        CompleteDate = timeProvider.ConvertToUserTimezone(item.ChangeLog.CreatedDate, timeZoneInfo),
                        StartDate = DateTime.MinValue,
                        Issue = item.ChangeLog.Issue.Adapt(),
                        Author = item.Author,
                        Source = eventSource
					};
                }
                // 2. Это последний элемент и он не завершается
                else if (i == (issueChangeLogItems.Count - 1))
                {
                    yield return new UserEvent()
                    {
                        CompleteDate = DateTime.MaxValue,
                        StartDate = timeProvider.ConvertToUserTimezone(item.ChangeLog.CreatedDate, timeZoneInfo),
                        Issue = item.ChangeLog.Issue.Adapt(),
                        Author = item.Author,
                        Source = eventSource
					};
                }
                // 3. Обычный случай когда после FromInProgress следует ToInProgress
                else
                {
                    yield return new UserEvent()
                    {
                        CompleteDate = timeProvider.ConvertToUserTimezone(issueChangeLogItems[i + 1].ChangeLog.CreatedDate, timeZoneInfo),
                        StartDate = timeProvider.ConvertToUserTimezone(item.ChangeLog.CreatedDate, timeZoneInfo),
                        Issue = item.ChangeLog.Issue.Adapt(),
                        Author = item.Author,
                        Source = eventSource
					};
                }

                i += 2;
            }
        }

        public static IEnumerable<UserEvent> ConvertTo(
            this List<IssueCommentDto> comments,
            ITimeProvider timeProvider,
            TimeZoneInfo timeZoneInfo,
            EventSource source,
            TimeSpan time)
        {
            foreach (var comment in comments)
            {
                var createdDate = timeProvider.ConvertToUserTimezone(comment.CreatedDate, timeZoneInfo);
                yield return new UserEvent()
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
