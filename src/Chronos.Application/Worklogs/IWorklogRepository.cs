using Chronos.Application.Worklogs.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Worklogs
{
    public interface IWorklogRepository
    {
        Task AddAsync(AddedWorklogDto worklog, CancellationToken cancellationToken = default);
    }
}
