using Microsoft.Extensions.Logging;
using Chronos.Application.Worklogs;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Worklogs;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Mock
{
    internal class MockWorklogRepository : IWorklogRepository
    {
        private readonly ILogger<MockWorklogRepository> _logger;

        public MockWorklogRepository(ILogger<MockWorklogRepository> logger)
        {
            _logger = logger;
        }

        public Task AddAsync(AddedWorklogDto worklog, CancellationToken cancellationToken = default)
        {
            MockWorklogStorage.IssueWorklogs.Add(new IssueWorklog
            {
                StartDate = worklog.StartedAt,
                Issue = new Issue(){Key = worklog.IssueKey},
                TimeSpent = worklog.ElapsedTime,
                CompleteDate = worklog.StartedAt.Add(worklog.ElapsedTime)
            });
            _logger.LogInformation("Worklog added successfully. {@entity}", worklog);
            return Task.CompletedTask;
        }
    }
}
