using System.Collections.Generic;

namespace Chronos.Application.Extensions.YandexCalendar.Dto
{
    public record YandexCalendarSettingsDto(
        string Login,
        string AppPassword,
        IReadOnlyList<string> ExcludedPhrases,
        IReadOnlyList<YandexCalendarIssueMapping> IssueMappings);
}
