using MediatR;
using System;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Authentication.Commands
{
    public class BearerLogin
    {
        public class Command : IRequest<Model>
        {
            public Command(BearerLoginRequest request)
            {
                _request = request;
            }

            private readonly BearerLoginRequest _request;
            public BearerLoginRequest Request => _request;
        }

        public class Model
        {
            public LoginResponse Response { get; set; }
        }

        public class Handler : IRequestHandler<Command, Model>
        {
            private readonly IAuthenticationService _authenticationService;

            public Handler(IAuthenticationService authenticationService)
            {
                _authenticationService = authenticationService;
            }

            public async Task<Model> Handle(
                Command command,
                CancellationToken cancellationToken)
            {
                try
                {
                    var loginResult = await _authenticationService.LoginAsync(command.Request);
                    return new Model { Response = loginResult };
                }
                catch (AuthenticationException e)
                {
                    throw new Exception($"Authentication exception [{command?.Request?.Token}]") { Source = e.Source };
                }
            }
        }
    }
}