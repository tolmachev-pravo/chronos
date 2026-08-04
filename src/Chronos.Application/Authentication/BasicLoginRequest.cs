using System.ComponentModel.DataAnnotations;

namespace Chronos.Application.Authentication
{
    public class BasicLoginRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
