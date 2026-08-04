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
    public class EnsureJiraExtensionHandlerTests
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
        public async Task Handle_WithExistingExtension_DoesNothing()
        {
            _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.Jira, CancellationToken.None))
                     .ReturnsAsync(new UserExtension { Username = "alice", Type = ExtensionType.Jira });

            await CreateHandler().Handle(
                new EnsureJiraExtension.Command("alice", HasLegacyFilter: true, LegacyCommentWorklogTime: TimeSpan.FromMinutes(20)),
                CancellationToken.None);

            _repoMock.Verify(r => r.UpsertAsync(It.IsAny<UserExtension>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_ForNewUser_CreatesDefaults()
        {
            SetupMissingExtension();

            await CreateHandler().Handle(
                new EnsureJiraExtension.Command("alice", HasLegacyFilter: false, LegacyCommentWorklogTime: null),
                CancellationToken.None);

            Assert.That(_saved, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_saved!.IsEnabled, Is.True);
                Assert.That(
                    JiraExtensionSettingsSerializer.Deserialize(_saved.Settings),
                    Is.EqualTo(JiraExtensionSettingsDto.Default));
            });
        }

        [Test]
        public async Task Handle_ForLegacyUserWithCommentTime_KeepsCommentsAndTesterEnabled()
        {
            SetupMissingExtension();

            await CreateHandler().Handle(
                new EnsureJiraExtension.Command("alice", HasLegacyFilter: true, LegacyCommentWorklogTime: TimeSpan.FromMinutes(20)),
                CancellationToken.None);

            var settings = JiraExtensionSettingsSerializer.Deserialize(_saved!.Settings);
            Assert.Multiple(() =>
            {
                Assert.That(settings.AssigneeEventsEnabled, Is.True);
                Assert.That(settings.TesterEventsEnabled, Is.True);
                Assert.That(settings.CommentEventsEnabled, Is.True);
                Assert.That(settings.CommentWorklogTime, Is.EqualTo(TimeSpan.FromMinutes(20)));
            });
        }

        [Test]
        public async Task Handle_ForLegacyUserWithoutCommentTime_KeepsCommentsDisabled()
        {
            SetupMissingExtension();

            await CreateHandler().Handle(
                new EnsureJiraExtension.Command("alice", HasLegacyFilter: true, LegacyCommentWorklogTime: TimeSpan.Zero),
                CancellationToken.None);

            var settings = JiraExtensionSettingsSerializer.Deserialize(_saved!.Settings);
            Assert.Multiple(() =>
            {
                Assert.That(settings.CommentEventsEnabled, Is.False);
                Assert.That(settings.TesterEventsEnabled, Is.True);
            });
        }

        [Test]
        public async Task Handle_WithoutUsername_DoesNothing()
        {
            await CreateHandler().Handle(
                new EnsureJiraExtension.Command(string.Empty, HasLegacyFilter: false, LegacyCommentWorklogTime: null),
                CancellationToken.None);

            _repoMock.Verify(r => r.GetAsync(It.IsAny<string>(), It.IsAny<ExtensionType>(), It.IsAny<CancellationToken>()), Times.Never);
            _repoMock.Verify(r => r.UpsertAsync(It.IsAny<UserExtension>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private void SetupMissingExtension()
            => _repoMock.Setup(r => r.GetAsync("alice", ExtensionType.Jira, CancellationToken.None))
                        .ReturnsAsync((UserExtension?)null);

        private EnsureJiraExtension.Handler CreateHandler() => new(_repoMock.Object);
    }
}
