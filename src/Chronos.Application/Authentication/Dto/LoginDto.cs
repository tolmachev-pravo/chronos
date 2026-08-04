using System;
using Chronos.Domain.Models.Abstract;

namespace Chronos.Application.Authentication.Dto
{
    public class LoginDto : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string PersonalAccessToken { get; set; }
        public AuthenticationType AuthenticationType { get; set; }
        public Guid Key => Id;

        public static LoginDto Create(BasicLoginRequest request)
        {
            return new LoginDto
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Password = request.Password,
                AuthenticationType = AuthenticationType.Basic
            };
        }

        public static LoginDto Create(BearerLoginRequest request, LoginResponse response)
        {
            return new LoginDto
            {
                Id = Guid.NewGuid(),
                PersonalAccessToken = request.Token,
                AuthenticationType = AuthenticationType.Bearer,
                Username = response.Username
            };
        }
    }
}
