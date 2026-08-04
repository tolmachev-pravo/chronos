using Microsoft.AspNetCore.DataProtection;
using Chronos.Application.Security;

namespace Chronos.Infrastructure.Security
{
    public class DataProtectionSecretProtector : ISecretProtector
    {
        private readonly IDataProtector _protector;

        public DataProtectionSecretProtector(IDataProtectionProvider provider)
            => _protector = provider.CreateProtector("UserExtensions.Secrets");

        public string Protect(string plaintext) => _protector.Protect(plaintext);
        public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
    }
}
