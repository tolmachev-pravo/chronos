using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Chronos.Application.Authentication;
using Chronos.Application.Authentication.Dto;
using Chronos.Web.Components.Common;
using Chronos.Web.Shared;
using System;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Authentication
{
    public partial class Login : ComponentBase
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IMediator Mediator { get; set; }
        [Inject] private ILoginMemoryCache LoginMemoryCache { get; set; }
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }

        private readonly ComponentModel Model = ComponentModel.Create();

        public async Task BasicOnKeyUp(KeyboardEventArgs keyboardEventArgs)
        {
            switch (keyboardEventArgs.Code)
            {
                case "Enter":
                case "NumpadEnter":
                    await BasicLogin();
                    break;
            }
        }

        public async Task BearerOnKeyUp(KeyboardEventArgs keyboardEventArgs)
        {
            switch (keyboardEventArgs.Code)
            {
                case "Enter":
                case "NumpadEnter":
                    await BearerLogin();
                    break;
            }
        }

        private async Task BasicLogin()
        {
            async Task<LoginDto> loginFunction()
            {
                await Mediator.Send(new Application.Authentication.Commands.BasicLogin.Command(Model.BasicLoginRequest));
                return LoginDto.Create(Model.BasicLoginRequest);
            }
            await BaseLogin(loginFunction);
        }

        private async Task BearerLogin()
        {
            async Task<LoginDto> loginFunction()
            {
                var response = await Mediator.Send(new Application.Authentication.Commands.BearerLogin.Command(Model.BearerLoginRequest));
                return LoginDto.Create(Model.BearerLoginRequest, response.Response);
            }
            await BaseLogin(loginFunction);
        }

        private async Task BaseLogin(Func<Task<LoginDto>> loginFunction)
        {
            try
            {
                Model.StateTo(ComponentModelState.InProgress);
                var loginDto = await loginFunction.Invoke();
                LoginMemoryCache.TryAdd(loginDto);
                NavigationManager.NavigateTo($"/login?key={loginDto.Id}", true);
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

            public BasicLoginRequest BasicLoginRequest { get; set; } = new BasicLoginRequest();
            public BearerLoginRequest BearerLoginRequest { get; set; } = new BearerLoginRequest();
        }
    }
}
