using Moq;
using NUnit.Framework;
using Chronos.Application.Extensions;
using Chronos.Application.Extensions.YandexCalendar;
using Chronos.Application.Extensions.YandexCalendar.Dto;
using Chronos.Application.Extensions.YandexCalendar.Queries;
using Chronos.Domain.Entities.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.UnitTests.Application.Extensions.YandexCalendar
{
    [TestFixture]
    public class GetYandexCalendarSettingsHandlerTests
    {
        private Mock<IUserExtensionRepository> _repoMock = null!;
        private Mock<IYandexCalendarSettingsProvider> _providerMock = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IUserExtensionRepository>();
            _providerMock = new Mock<IYandexCalendarSettingsProvider>();
        }

        [Test]
        public async Task Handle_ReturnsEnabledWithSettings_WhenExtensionEnabled()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.YandexCalendar, CancellationToken.None))
                     .ReturnsAsync(new UserExtension { IsEnabled = true });

            var settings = new YandexCalendarSettingsDto("user@yandex.ru", "secret", new List<string>(), new List<YandexCalendarIssueMapping>());
            _providerMock.Setup(p => p.GetSettingsAsync("alice", CancellationToken.None))
                         .ReturnsAsync(settings);

            var handler = new GetYandexCalendarSettings.Handler(_repoMock.Object, _providerMock.Object);
            var result = await handler.Handle(new GetYandexCalendarSettings.Query("alice"), CancellationToken.None);

            Assert.That(result.IsEnabled, Is.True);
            Assert.That(result.Settings, Is.Not.Null);
            Assert.That(result.Settings!.Login, Is.EqualTo("user@yandex.ru"));
        }

        [Test]
        public async Task Handle_ReturnsDisabledWithSettings_WhenExtensionDisabled()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.YandexCalendar, CancellationToken.None))
                     .ReturnsAsync(new UserExtension { IsEnabled = false });

            var settings = new YandexCalendarSettingsDto("user@yandex.ru", "secret", new List<string>(), new List<YandexCalendarIssueMapping>());
            _providerMock.Setup(p => p.GetSettingsAsync("alice", CancellationToken.None))
                         .ReturnsAsync(settings);

            var handler = new GetYandexCalendarSettings.Handler(_repoMock.Object, _providerMock.Object);
            var result = await handler.Handle(new GetYandexCalendarSettings.Query("alice"), CancellationToken.None);

            Assert.That(result.IsEnabled, Is.False);
            Assert.That(result.Settings, Is.Not.Null);
        }

        [Test]
        public async Task Handle_ReturnsDisabledWithNullSettings_WhenExtensionNotConfigured()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.YandexCalendar, CancellationToken.None))
                     .ReturnsAsync((UserExtension?)null);

            var handler = new GetYandexCalendarSettings.Handler(_repoMock.Object, _providerMock.Object);
            var result = await handler.Handle(new GetYandexCalendarSettings.Query("alice"), CancellationToken.None);

            Assert.That(result.IsEnabled, Is.False);
            Assert.That(result.Settings, Is.Null);
        }
    }
}
