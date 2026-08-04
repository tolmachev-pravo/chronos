using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Worklogs;
using System;

namespace Chronos.Application.Worklogs.Dto
{
    internal class WorklogDto : IWorklog
    {
        public DateTime StartDate { get; set; }
        public DateTime CompleteDate { get; set; }
        public TimeSpan TimeSpent => CompleteDate - StartDate;
        public IIssue Issue { get; set; }
        public string Author { get; set; }
        public WorklogSource Source { get; set; }
    }
}
