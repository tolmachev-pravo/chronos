using Chronos.Domain.Models.Issues;
using System;

namespace Chronos.Domain.Models.Worklogs
{
    public interface IWorklog
    {
        DateTime StartDate { get; set; }
        DateTime CompleteDate { get; set; }
        TimeSpan TimeSpent { get; }
        IIssue Issue { get; set; }
        string Author { get; set; }
        WorklogSource Source { get; set; }
    }
}
