using Moq;
using NUnit.Framework;
using Chronos.Application.Extensions;
using Chronos.Application.Extensions.Jira;
using Chronos.Application.Extensions.Jira.Commands;
using Chronos.Application.Extensions.Jira.Dto;
using Chronos.Domain.Entities.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.UnitTests.Application.Extensions.Jira
{
    [TestFixture]
    public class UpsertJiraExtensionHandlerTests
    {
        private Mock<IUserExtensionRepository> _repoMock = null!;
        private UserExtension? _saved;

        [SetUp]
        public void SetUp()
        {
            _saved = null;
            _repoMock = new Mock<IUserExtensionRepository>();
            _repoMock.Setup(r => r.UpsertAsync(It.IsAny<UserExtension>(), CancellationToken.None))
                     .Callback<UserExtension, CancellationToken>((entity, _) => _saved = entity)
                     .Returns(Task.CompletedTask);
        }

        [Test]
        public async Task Handle_WithoutExistingExtension_CreatesRecord()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.Jira, CancellationToken.None))
                     .ReturnsAsync((UserExtension?)null);

            var settings = new JiraExtensionSettingsDto(true, true, TimeSpan.FromMinutes(15), true);
            await CreateHandler().Handle(
                new UpsertJiraExtension.Command("alice", settings, IsEnabled: true),
                CancellationToken.None);

            Assert.That(_saved, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_saved!.Username, Is.EqualTo("alice"));
                Assert.That(_saved.Type, Is.EqualTo(ExtensionType.Jira));
                Assert.That(_saved.IsEnabled, Is.True);
                Assert.That(JiraExtensionSettingsSerializer.Deserialize(_saved.Settings), Is.EqualTo(settings));
            });
        }

        [Test]
        public async Task Handle_WithExistingExtension_UpdatesSettingsAndKeepsCreatedAt()
        {
            var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.Jira, CancellationToken.None))
                     .ReturnsAsync(new UserExtension
                     {
                         Username = "alice",
                         Type = ExtensionType.Jira,
                         IsEnabled = true,
                         CreatedAt = createdAt,
                         Settings = JiraExtensionSettingsSerializer.Serialize(JiraExtensionSettingsDto.Default)
                     });

            var settings = new JiraExtensionSettingsDto(true, false, TimeSpan.Zero, true);
            await CreateHandler().Handle(
                new UpsertJiraExtension.Command("alice", settings, IsEnabled: false),
                CancellationToken.None);

            Assert.That(_saved, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_saved!.CreatedAt, Is.EqualTo(createdAt));
                Assert.That(_saved.UpdatedAt, Is.Not.Null);
                Assert.That(_saved.IsEnabled, Is.False);
                Assert.That(JiraExtensionSettingsSerializer.Deserialize(_saved.Settings), Is.EqualTo(settings));
            });
        }

        private UpsertJiraExtension.Handler CreateHandler() => new(_repoMock.Object);
    }
}
