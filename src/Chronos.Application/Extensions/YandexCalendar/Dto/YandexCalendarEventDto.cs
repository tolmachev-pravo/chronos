using System;

namespace Chronos.Application.Extensions.YandexCalendar.Dto
{
    public record YandexCalendarEventDto(
        string Summary,
        DateTime Start,
        DateTime End,
        string? JiraIssueKeyHint);
}
