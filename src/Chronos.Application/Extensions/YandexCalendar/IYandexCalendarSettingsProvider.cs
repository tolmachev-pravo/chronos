using Chronos.Application.Extensions.YandexCalendar.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Extensions.YandexCalendar
{
    public interface IYandexCalendarSettingsProvider
    {
        Task<YandexCalendarSettingsDto?> GetSettingsAsync(string username, CancellationToken ct = default);
    }
}
