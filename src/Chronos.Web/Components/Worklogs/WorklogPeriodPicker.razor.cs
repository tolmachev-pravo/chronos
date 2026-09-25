using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogPeriodPicker : ComponentBase
    {
        private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

        /// <summary>The period the picker opens on.</summary>
        [Parameter] public WorklogPeriod Initial { get; set; }

        [Parameter] public EventCallback<WorklogPeriod> OnPicked { get; set; }

        private DateRange _range;

        private DateTime Today => DateTime.Today;

        private bool CanPick => _range?.Start is not null && _range.End is not null;

        protected override void OnParametersSet()
        {
            _range ??= Initial is null ? null : new DateRange(Initial.Start, Initial.End);
        }

        private Task PickRangeAsync() =>
            OnPicked.InvokeAsync(WorklogPeriod.Custom(_range.Start.Value, _range.End.Value, Today));

        /// <summary>The month of the chosen start, or the current one.</summary>
        private Task PickMonthAsync()
        {
            var day = _range?.Start ?? Today;
            var month = WorklogPeriod.ThisMonth(Today);
            var picked = month.Shift((day.Year - Today.Year) * 12 + day.Month - Today.Month, Today);
            return OnPicked.InvokeAsync(picked);
        }
    }
}
