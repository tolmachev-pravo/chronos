using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Chronos.Application.Users;
using Chronos.Web.Authentication;
using System.Security.Claims;

namespace Chronos.UnitTests.Web.Authentication
{
    [TestFixture]
    public class UserProvisioningMiddlewareTests
    {
        private Mock<IUserRepository> _userRepository;

        [SetUp]
        public void SetUp()
        {
            _userRepository = new Mock<IUserRepository>();
        }

        private static HttpContext CreateContext(ClaimsPrincipal user)
        {
            return new DefaultHttpContext { User = user };
        }

        private static ClaimsPrincipal AuthenticatedUser(string username)
        {
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, username) },
                authenticationType: "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private UserProvisioningMiddleware CreateMiddleware(RequestDelegate next)
        {
            return new UserProvisioningMiddleware(next, NullLogger<UserProvisioningMiddleware>.Instance);
        }

        [Test]
        public async Task InvokeAsync_Should_ProvisionUserOnce_AcrossRepeatedRequests()
        {
            var nextCallCount = 0;
            RequestDelegate next = _ => { nextCallCount++; return Task.CompletedTask; };
            var middleware = CreateMiddleware(next);

            await middleware.InvokeAsync(CreateContext(AuthenticatedUser("john")), _userRepository.Object);
            await middleware.InvokeAsync(CreateContext(AuthenticatedUser("john")), _userRepository.Object);

            _userRepository.Verify(
                repository => repository.EnsureUserExistsAsync("john", It.IsAny<CancellationToken>()),
                Times.Once());
            Assert.That(nextCallCount, Is.EqualTo(2));
        }

        [Test]
        public async Task InvokeAsync_Should_SkipProvisioning_WhenUserIsAnonymous()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
            var middleware = CreateMiddleware(next);

            await middleware.InvokeAsync(CreateContext(new ClaimsPrincipal(new ClaimsIdentity())), _userRepository.Object);

            _userRepository.Verify(
                repository => repository.EnsureUserExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never());
            Assert.That(nextCalled, Is.True);
        }

        [Test]
        public async Task InvokeAsync_Should_NotBreakPipeline_WhenRepositoryThrows()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
            _userRepository
                .Setup(repository => repository.EnsureUserExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("db down"));
            var middleware = CreateMiddleware(next);

            await middleware.InvokeAsync(CreateContext(AuthenticatedUser("john")), _userRepository.Object);

            Assert.That(nextCalled, Is.True);
        }
    }
}
