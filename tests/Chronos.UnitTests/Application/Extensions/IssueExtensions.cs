using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Issues;

namespace Chronos.UnitTests.Application.Extensions
{
	static class IssueExtensions
	{
		public static WorkingDayWorklog CreateWorkingDayWorklog(
			this IIssue issue,
			DateTime date,
			TimeSpan from,
			TimeSpan to)
		{
			return new WorkingDayWorklog
			{
				Issue = issue,
				RawStartDate = date.Add(from),
				RawCompleteDate = date.Add(to)
			};
		}
	}
}
