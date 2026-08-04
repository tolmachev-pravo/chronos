using System.ComponentModel.DataAnnotations;

namespace Chronos.Application.Authentication
{
    public class BearerLoginRequest
    {
        [Required]
        public string Token { get; set; }
    }
}
