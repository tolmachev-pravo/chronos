using Chronos.Domain.Models.Issues;
using System;

namespace Chronos.Domain.Models.Worklogs
{
    /// <summary>
    /// Time actually logged against an issue. Traces of activity an estimate is derived
    /// from are <see cref="Events.IEvent"/> instead. See issue #299.
    /// </summary>
    public interface IWorklog
    {
        DateTime StartDate { get; set; }
        DateTime CompleteDate { get; set; }
        TimeSpan TimeSpent { get; }
        IIssue Issue { get; set; }
        string Author { get; set; }
    }
}
