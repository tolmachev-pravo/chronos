using NUnit.Framework;
using Chronos.Application.Extensions.YandexCalendar;
using Chronos.Infrastructure.Extensions.YandexCalendar;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.UnitTests.Infrastructure.Extensions.YandexCalendar
{
    [TestFixture]
    public class YandexCalDavServiceTests
    {
        // A minimal CalDAV REPORT response with one VEVENT
        private const string FakeCalDavResponse = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<multistatus xmlns=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:caldav"">
  <response>
    <href>/calendars/user@yandex.ru/events-default/event1.ics</href>
    <propstat>
      <prop>
        <C:calendar-data>BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
DTSTART:20260604T100000Z
DTEND:20260604T110000Z
SUMMARY:Team sync PROJ-42
DESCRIPTION:Discuss PROJ-42 progress
END:VEVENT
END:VCALENDAR</C:calendar-data>
      </prop>
      <status>HTTP/1.1 200 OK</status>
    </propstat>
  </response>
</multistatus>";

        [Test]
        public async Task GetEventsAsync_ParsesEventAndJiraHint()
        {
            var svc = new YandexCalDavService(new HttpClient(new FakeHttpMessageHandler(FakeCalDavResponse)));

            var result = await svc.GetEventsAsync(
                new YandexCalendarCredentials("user@yandex.ru", "pw"),
                new DateOnly(2026, 6, 4),
                TimeZoneInfo.Utc);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Summary, Is.EqualTo("Team sync PROJ-42"));
            Assert.That(result[0].JiraIssueKeyHint, Is.EqualTo("PROJ-42"));
        }

        // The same response with everything an invitation can carry. See issue #156.
        private const string DetailedCalDavResponse = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<multistatus xmlns=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:caldav"">
  <response>
    <href>/calendars/user@yandex.ru/events-default/event1.ics</href>
    <propstat>
      <prop>
        <C:calendar-data>BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
DTSTART:20260604T100000Z
DTEND:20260604T110000Z
SUMMARY:Backlog grooming PROJ-42
DESCRIPTION:Why PROJ-42 has been open for three weeks
LOCATION:Saturn room
ORGANIZER;CN=Anna Kovaleva:mailto:anna@example.com
ATTENDEE;CN=Dmitry Tolmachev:mailto:dmitry@example.com
ATTENDEE:mailto:nameless@example.com
END:VEVENT
END:VCALENDAR</C:calendar-data>
      </prop>
      <status>HTTP/1.1 200 OK</status>
    </propstat>
  </response>
</multistatus>";

        [Test]
        public async Task GetEventsAsync_ParsesTheRestOfTheInvitation()
        {
            var svc = new YandexCalDavService(new HttpClient(new FakeHttpMessageHandler(DetailedCalDavResponse)));

            var result = await svc.GetEventsAsync(
                new YandexCalendarCredentials("user@yandex.ru", "pw"),
                new DateOnly(2026, 6, 4),
                TimeZoneInfo.Utc);

            Assert.That(result, Has.Count.EqualTo(1));
            var meeting = result[0];
            Assert.Multiple(() =>
            {
                Assert.That(meeting.Location, Is.EqualTo("Saturn room"));
                Assert.That(meeting.Description, Is.EqualTo("Why PROJ-42 has been open for three weeks"));
                Assert.That(meeting.Organizer, Is.EqualTo("Anna Kovaleva"));
                // An attendee named only by address falls back to the address itself,
                // without the mailto scheme nobody wants to read.
                Assert.That(meeting.Attendees, Is.EqualTo(new[] { "Dmitry Tolmachev", "nameless@example.com" }));
            });
        }

        [Test]
        public async Task GetEventsAsync_LeavesTheInvitationFieldsUnset_WhenTheEventCarriesNone()
        {
            var svc = new YandexCalDavService(new HttpClient(new FakeHttpMessageHandler(FakeCalDavResponse)));

            var result = await svc.GetEventsAsync(
                new YandexCalendarCredentials("user@yandex.ru", "pw"),
                new DateOnly(2026, 6, 4),
                TimeZoneInfo.Utc);

            Assert.Multiple(() =>
            {
                Assert.That(result[0].Organizer, Is.Null);
                Assert.That(result[0].Location, Is.Null);
                Assert.That(result[0].Attendees, Is.Empty);
            });
        }

        [Test]
        public async Task GetEventsAsync_ReturnsEmpty_WhenNoEvents()
        {
            const string emptyResponse = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<multistatus xmlns=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:caldav"">
</multistatus>";
            var svc = new YandexCalDavService(new HttpClient(new FakeHttpMessageHandler(emptyResponse)));

            var result = await svc.GetEventsAsync(
                new YandexCalendarCredentials("user@yandex.ru", "pw"),
                new DateOnly(2026, 6, 4),
                TimeZoneInfo.Utc);

            Assert.That(result, Is.Empty);
        }

        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _response;
            public FakeHttpMessageHandler(string response) => _response = response;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken ct)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.MultiStatus)
                {
                    Content = new StringContent(_response, System.Text.Encoding.UTF8, "application/xml")
                });
        }
    }
}
