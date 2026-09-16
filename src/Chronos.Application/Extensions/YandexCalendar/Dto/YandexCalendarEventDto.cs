using System;
using System.Collections.Generic;

namespace Chronos.Application.Extensions.YandexCalendar.Dto
{
    /// <summary>
    /// A meeting as the calendar describes it. Everything after the issue key hint is what
    /// the row shows when its details are expanded, and is optional: an invitation may name
    /// no organizer, no place and nobody. See issue #156.
    /// </summary>
    public record YandexCalendarEventDto(
        string Summary,
        DateTime Start,
        DateTime End,
        string? JiraIssueKeyHint,
        string? Description = null,
        string? Organizer = null,
        string? Location = null,
        IReadOnlyList<string>? Attendees = null);
}
