using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Chronos.Application.Extensions.YandexCalendar;
using Chronos.Application.Extensions.YandexCalendar.Dto;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Chronos.Infrastructure.Extensions.YandexCalendar
{
    public class YandexCalDavService : IYandexCalendarService
    {
        private static readonly XNamespace CalDavNs = "urn:ietf:params:xml:ns:caldav";
        private static readonly Regex JiraKeyRegex = new(@"\b[A-Z]+-\d+\b", RegexOptions.Compiled);
        private const string CalDavBaseUrl = "https://caldav.yandex.ru/calendars/{0}/events-default/";

        private readonly HttpClient _http;

        public YandexCalDavService(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<YandexCalendarEventDto>> GetEventsAsync(
            YandexCalendarCredentials credentials,
            DateOnly date,
            TimeZoneInfo userTimeZone,
            CancellationToken ct = default)
        {
            var url = string.Format(CalDavBaseUrl, credentials.Login);
            var body = BuildReportBody(date, userTimeZone);

            var request = new HttpRequestMessage(new HttpMethod("REPORT"), url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/xml")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{credentials.Login}:{credentials.AppPassword}")));
            request.Headers.Add("Depth", "1");

            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var xml = await response.Content.ReadAsStringAsync(ct);
            return ParseCalDavResponse(xml, date);
        }

        private static string BuildReportBody(DateOnly date, TimeZoneInfo userTimeZone)
        {
            var start = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), userTimeZone).ToString("yyyyMMdd'T'HHmmss") + "Z";
            var end = TimeZoneInfo.ConvertTimeToUtc(date.AddDays(1).ToDateTime(TimeOnly.MinValue), userTimeZone).ToString("yyyyMMdd'T'HHmmss") + "Z";
            return $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<C:calendar-query xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:caldav"">
  <D:prop><D:getetag/><C:calendar-data/></D:prop>
  <C:filter>
    <C:comp-filter name=""VCALENDAR"">
      <C:comp-filter name=""VEVENT"">
        <C:time-range start=""{start}"" end=""{end}""/>
      </C:comp-filter>
    </C:comp-filter>
  </C:filter>
</C:calendar-query>";
        }

        private static IReadOnlyList<YandexCalendarEventDto> ParseCalDavResponse(string xml, DateOnly date)
        {
            var doc = XDocument.Parse(xml);
            var calendarDataElements = doc.Descendants(CalDavNs + "calendar-data");
            var result = new List<YandexCalendarEventDto>();

            var rangeStart = new CalDateTime(date.ToDateTime(TimeOnly.MinValue));
            var rangeEnd = new CalDateTime(date.AddDays(1).ToDateTime(TimeOnly.MinValue));

            foreach (var element in calendarDataElements)
            {
                var icalText = element.Value;
                if (string.IsNullOrWhiteSpace(icalText)) continue;

                var calendar = Calendar.Load(icalText);
                var occurrences = calendar.GetOccurrences<CalendarEvent>(rangeStart, rangeEnd);

                foreach (var occurrence in occurrences)
                {
                    if (occurrence.Source is not CalendarEvent vevent) continue;

                    var start = occurrence.Period.StartTime.AsUtc;
                    var end = occurrence.Period.EndTime?.AsUtc ?? start;
                    var summary = vevent.Summary ?? string.Empty;
                    var description = vevent.Description ?? string.Empty;
                    var match = JiraKeyRegex.Match(summary + " " + description);
                    var hint = match.Success ? match.Value : null;

                    result.Add(new YandexCalendarEventDto(summary, start, end, hint));
                }
            }

            return result;
        }
    }
}
