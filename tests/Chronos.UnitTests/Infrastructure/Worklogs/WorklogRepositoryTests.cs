using Moq;
using Chronos.Application.Authentication;
using Chronos.Application.Storage;
using Chronos.Application.Time;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Users;
using Chronos.Infrastructure.Jira;
using Chronos.Infrastructure.Worklogs;

namespace Chronos.UnitTests.Infrastructure.Worklogs
{
    [TestFixture]
    public class WorklogRepositoryTests
    {
        private WorklogRepository _worklogRepository;
        private Mock<IIdentityService> _identityService;
        private Mock<IJiraService> _jiraService;
        private Mock<IStorage<string, UserProfile>> _storage;
        private Mock<ITimeProvider> _timeProvider;

        [SetUp]
        public void SetUp()
        {
            _identityService = new Mock<IIdentityService>();
            _identityService
                .Setup(mock => mock.GetCurrentUserAsync())
                .Returns(Task.FromResult(new User
                {
                    Username = "user-key"
                }));

            _timeProvider = new Mock<ITimeProvider>();
            _storage = new Mock<IStorage<string, UserProfile>>();
            _storage
                .Setup(mock => mock.GetValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserProfile
                {
                    TimeZoneId = TimeZoneInfo.Utc.Id
                }));

            _jiraService = new Mock<IJiraService>();

            _worklogRepository = new WorklogRepository(
                _jiraService.Object,
                _timeProvider.Object,
                _storage.Object,
                _identityService.Object);
        }

        [Test]
        public async Task AddAsync_Should_ConvertDateTimeToServerTimezone()
        {
            // Arrange
            var worklog = new AddedWorklogDto { StartedAt = new DateTime(2023, 5, 1, 23, 58, 23) };

            // Act
            await _worklogRepository.AddAsync(worklog);

            // Assert
            _timeProvider.Verify(timeProvider =>
                timeProvider.ConvertToServerTimezone(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()), Times.Once());
        }

        [Test]
        public async Task AddAsync_Should_CallJiraServiceAddWorklogAsync()
        {
            // Arrange
            var worklog = new AddedWorklogDto { StartedAt = new DateTime(2023, 5, 1, 23, 58, 23) };

            // Act
            await _worklogRepository.AddAsync(worklog);

            // Assert
            _jiraService.Verify(jiraService =>
                jiraService.AddWorklogAsync(It.IsAny<AddedWorklogDto>(), It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}

