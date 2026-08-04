using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;

namespace Chronos.Application.Worklogs
{
    public interface IWorklogDistributor
    {
        public IEnumerable<WorkingDay> DistributeByDays(
            IEnumerable<IWorklog> worklogs,
            DateTime firstDate,
            DateTime lastDate,
            WorkingDaySettings daySettings);
    }
}
