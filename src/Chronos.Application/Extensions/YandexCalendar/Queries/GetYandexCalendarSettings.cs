using MediatR;
using Chronos.Application.Extensions.YandexCalendar.Dto;
using Chronos.Domain.Entities.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Extensions.YandexCalendar.Queries
{
    public class GetYandexCalendarSettings
    {
        public record Query(string Username) : IRequest<YandexCalendarExtensionDto>;

        public class Handler : IRequestHandler<Query, YandexCalendarExtensionDto>
        {
            private readonly IUserExtensionRepository _repository;
            private readonly IYandexCalendarSettingsProvider _settingsProvider;

            public Handler(IUserExtensionRepository repository, IYandexCalendarSettingsProvider settingsProvider)
            {
                _repository = repository;
                _settingsProvider = settingsProvider;
            }

            public async Task<YandexCalendarExtensionDto> Handle(Query request, CancellationToken ct)
            {
                var entity = await _repository.GetAsync(request.Username, ExtensionType.YandexCalendar, ct);
                if (entity is null)
                    return new YandexCalendarExtensionDto(false, null);

                var settings = await _settingsProvider.GetSettingsAsync(request.Username, ct);
                return new YandexCalendarExtensionDto(entity.IsEnabled, settings);
            }
        }
    }
}
