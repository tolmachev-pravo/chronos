using System.Threading.Tasks;

namespace Chronos.Application.Authentication
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> LoginAsync(BasicLoginRequest request);
        Task<LoginResponse> LoginAsync(BearerLoginRequest request);
    }
}
