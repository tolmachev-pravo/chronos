using Microsoft.Extensions.Options;
using Chronos.Application.Calendar;
using Chronos.Domain.Entities.Calendar;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.WorkingDays
{
    /// <summary>The production calendar of Russia as xmlcalendar.ru publishes it. See issue #310.</summary>
    public class XmlCalendarSource : IProductionCalendarSource
    {
        private readonly HttpClient _httpClient;
        private readonly ProductionCalendarOptions _options;

        public XmlCalendarSource(HttpClient httpClient, IOptions<ProductionCalendarOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<IReadOnlyList<CalendarDay>> GetYearAsync(int year, CancellationToken ct = default)
        {
            var url = string.Format(CultureInfo.InvariantCulture, _options.Url, year);
            using var response = await _httpClient.GetAsync(url, ct);

            // A year is published once the government sets it; until then there is nothing.
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            return XmlCalendarParser.Parse(json);
        }
    }
}
