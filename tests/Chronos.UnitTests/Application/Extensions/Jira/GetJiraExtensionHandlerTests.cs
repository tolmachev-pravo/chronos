using Moq;
using NUnit.Framework;
using Chronos.Application.Extensions;
using Chronos.Application.Extensions.Jira;
using Chronos.Application.Extensions.Jira.Dto;
using Chronos.Application.Extensions.Jira.Queries;
using Chronos.Infrastructure.Extensions.Jira;
using Chronos.Domain.Entities.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.UnitTests.Application.Extensions.Jira
{
    [TestFixture]
    public class GetJiraExtensionHandlerTests
    {
        private Mock<IUserExtensionRepository> _repoMock = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IUserExtensionRepository>();
        }

        [Test]
        public async Task Handle_WithoutStoredExtension_ReturnsEnabledDefaults()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.Jira, CancellationToken.None))
                     .ReturnsAsync((UserExtension?)null);

            var result = await CreateHandler().Handle(new GetJiraExtension.Query("alice"), CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsEnabled, Is.True);
                Assert.That(result.Settings.AssigneeEventsEnabled, Is.True);
                Assert.That(result.Settings.CommentEventsEnabled, Is.False);
                Assert.That(result.Settings.TesterEventsEnabled, Is.False);
                Assert.That(result.Settings.CommentWorklogTime, Is.EqualTo(TimeSpan.Zero));
            });
        }

        [Test]
        public async Task Handle_WithStoredExtension_ReturnsStoredSettings()
        {
            var stored = new JiraExtensionSettingsDto(
                AssigneeEventsEnabled: false,
                CommentEventsEnabled: true,
                CommentWorklogTime: TimeSpan.FromMinutes(30),
                TesterEventsEnabled: true);

            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.Jira, CancellationToken.None))
                     .ReturnsAsync(new UserExtension
                     {
                         Username = "alice",
                         Type = ExtensionType.Jira,
                         IsEnabled = false,
                         Settings = JiraExtensionSettingsSerializer.Serialize(stored)
                     });

            var result = await CreateHandler().Handle(new GetJiraExtension.Query("alice"), CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsEnabled, Is.False);
                Assert.That(result.Settings, Is.EqualTo(stored));
            });
        }

        [Test]
        public async Task Handle_WithUnreadableSettings_FallsBackToDefaults()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.Jira, CancellationToken.None))
                     .ReturnsAsync(new UserExtension
                     {
                         Username = "alice",
                         Type = ExtensionType.Jira,
                         IsEnabled = true,
                         Settings = "{ not json"
                     });

            var result = await CreateHandler().Handle(new GetJiraExtension.Query("alice"), CancellationToken.None);

            Assert.That(result.Settings, Is.EqualTo(JiraExtensionSettingsDto.Default));
        }

        private GetJiraExtension.Handler CreateHandler() => new(new JiraExtensionProvider(_repoMock.Object));
    }
}
