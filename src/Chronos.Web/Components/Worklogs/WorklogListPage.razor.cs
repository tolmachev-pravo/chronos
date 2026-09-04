using MediatR;
using Microsoft.AspNetCore.Components;
using Chronos.Application.Authentication;
using Chronos.Application.Worklogs.Dto;
using Chronos.Application.Worklogs.Queries;
using Chronos.Web.Components.Common;
using Chronos.Web.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogListPage : ComponentBase
    {
        private readonly ComponentModel Model = ComponentModel.Create();

        [Inject] private IMediator Mediator { get; set; }
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }

        protected async Task SearchAsync(GetWorklogCollection.Query filter)
        {
            try
            {
                Model.StateTo(ComponentModelState.InProgress);
                var filterResult = await Mediator.Send(filter);
                Model.Items = filterResult.WorkingDays;
            }
            catch (JiraAuthenticationException e)
            {
                // The period was never read: Jira refused the user halfway through. What
                // an earlier search left on screen goes with it, so the page does not
                // keep showing a day as if nothing had happened. See issue #305.
                Model.Items = null;
                ErrorHandler.ProcessError(e);
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
            finally
            {
                Model.StateTo(ComponentModelState.Success);
            }
        }

        private class ComponentModel : BaseStateComponentModel
        {
            public static ComponentModel Create()
            {
                return new ComponentModel();
            }

            public IEnumerable<WorkingDay> Items { get; set; }
        }
    }
}
