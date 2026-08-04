using MediatR;
using Chronos.Application.Extensions.YandexCalendar.Dto;
using Chronos.Application.Storage;
using Chronos.Domain.Entities.Extensions;
using Chronos.Domain.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Extensions.YandexCalendar.Queries
{
    public class GetYandexCalendarEvents
    {
        public record Query(string Username, DateOnly Date)
            : IRequest<IReadOnlyList<YandexCalendarEventDto>>;

        public class Handler : IRequestHandler<Query, IReadOnlyList<YandexCalendarEventDto>>
        {
            private readonly IUserExtensionRepository _repository;
            private readonly IYandexCalendarSettingsProvider _settings;
            private readonly IYandexCalendarService _calendar;
            private readonly IStorage<string, UserProfile> _userProfileStorage;

            public Handler(
                IUserExtensionRepository repository,
                IYandexCalendarSettingsProvider settings,
                IYandexCalendarService calendar,
                IStorage<string, UserProfile> userProfileStorage)
            {
                _repository = repository;
                _settings = settings;
                _calendar = calendar;
                _userProfileStorage = userProfileStorage;
            }

            public async Task<IReadOnlyList<YandexCalendarEventDto>> Handle(Query request, CancellationToken ct)
            {
                var entity = await _repository.GetAsync(request.Username, ExtensionType.YandexCalendar, ct);
                if (entity is null || !entity.IsEnabled)
                    return Array.Empty<YandexCalendarEventDto>();

                var settings = await _settings.GetSettingsAsync(request.Username, ct);
                if (settings is null)
                    return Array.Empty<YandexCalendarEventDto>();

                var userProfile = await _userProfileStorage.GetValueAsync(request.Username, ct);
                var userTimeZone = userProfile?.TimeZoneInfo ?? TimeZoneInfo.Local;

                var utcEvents = await _calendar.GetEventsAsync(
                    new YandexCalendarCredentials(settings.Login, settings.AppPassword),
                    request.Date,
                    userTimeZone,
                    ct);

                return utcEvents
                    .Where(e => !settings.ExcludedPhrases.Any(p =>
                        e.Summary.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    .Select(e =>
                    {
                        var mapping = settings.IssueMappings.FirstOrDefault(m =>
                            string.Equals(e.Summary, m.Phrase, StringComparison.OrdinalIgnoreCase));
                        return e with
                        {
                            JiraIssueKeyHint = mapping?.IssueKey ?? e.JiraIssueKeyHint,
                            Start = TimeZoneInfo.ConvertTimeFromUtc(e.Start, userTimeZone),
                            End   = TimeZoneInfo.ConvertTimeFromUtc(e.End,   userTimeZone)
                        };
                    })
                    .ToList();
            }
        }
    }
}
