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
                        Source = eventSource,
                        // Entering the status happened before the changelog the period
                        // asked for, so there is nothing to name as the status before.
                        Details = new TransitionEventDetails
                        {
                            Status = statusName,
                            ToStatus = item.ToValue
                        }
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
                        Source = eventSource,
                        // Still in the status at the end of the period — no status to
                        // name as the one it went to.
                        Details = new TransitionEventDetails
                        {
                            Status = statusName,
                            FromStatus = item.FromValue
                        }
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
                        Source = eventSource,
                        Details = new TransitionEventDetails
                        {
                            Status = statusName,
                            FromStatus = item.FromValue,
                            ToStatus = issueChangeLogItems[i + 1].ToValue
                        }
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
            TimeSpan time,
            IJiraLinkGenerator linkGenerator)
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
                    Source = source,
                    Details = new CommentEventDetails
                    {
                        Body = comment.Body,
                        Author = comment.AuthorDisplayName ?? comment.Author,
                        Link = linkGenerator.GenerateComment(comment.Issue.Key, comment.Id)
                    }
                };
            }
        }
    }
}
