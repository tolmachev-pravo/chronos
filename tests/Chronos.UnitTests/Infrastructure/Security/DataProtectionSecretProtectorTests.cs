using Microsoft.AspNetCore.DataProtection;
using NUnit.Framework;
using Chronos.Infrastructure.Security;

namespace Chronos.UnitTests.Infrastructure.Security
{
    [TestFixture]
    public class DataProtectionSecretProtectorTests
    {
        [Test]
        public void ProtectThenUnprotect_ReturnsOriginalValue()
        {
            var provider = DataProtectionProvider.Create("test-app");
            var sut = new DataProtectionSecretProtector(provider);

            var original = "my-secret-password";
            var encrypted = sut.Protect(original);
            var decrypted = sut.Unprotect(encrypted);

            Assert.That(encrypted, Is.Not.EqualTo(original));
            Assert.That(decrypted, Is.EqualTo(original));
        }
    }
}
