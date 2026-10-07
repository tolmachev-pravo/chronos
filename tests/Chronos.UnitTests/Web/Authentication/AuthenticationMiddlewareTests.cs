using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Chronos.Application.Authentication;
using Chronos.Web.Authentication;
using AspNetAuthenticationService = Microsoft.AspNetCore.Authentication.IAuthenticationService;
using AuthenticationMiddleware = Chronos.Web.Authentication.AuthenticationMiddleware;

namespace Chronos.UnitTests.Web.Authentication
{
    [TestFixture]
    public class AuthenticationMiddlewareTests
    {
        private Mock<AspNetAuthenticationService> _authenticationService;

        [SetUp]
        public void SetUp()
        {
            _authenticationService = new Mock<AspNetAuthenticationService>();
        }

        private HttpContext CreateContext(string path, string? queryString = null)
        {
            var services = new ServiceCollection()
                .AddSingleton(_authenticationService.Object)
                .BuildServiceProvider();

            var context = new DefaultHttpContext { RequestServices = services };
            context.Request.Path = path;
            if (queryString != null)
            {
                context.Request.QueryString = new QueryString(queryString);
            }
            return context;
        }

        private static AuthenticationMiddleware CreateMiddleware() =>
            new(_ => Task.CompletedTask, Mock.Of<ILoginMemoryCache>());

        [Test]
        public async Task Logout_Should_SignOut_AndGoHome()
        {
            var context = CreateContext("/logout");

            await CreateMiddleware().Invoke(context);

            _authenticationService.Verify(
                service => service.SignOutAsync(context, null, null),
                Times.Once());
            Assert.That(context.Response.Headers.Location.ToString(), Is.EqualTo("/"));
        }

        [Test]
        public async Task Logout_Should_CarryTheRefusal_ToTheLoginPage()
        {
            var context = CreateContext("/logout", "?reason=refused");

            await CreateMiddleware().Invoke(context);

            _authenticationService.Verify(
                service => service.SignOutAsync(context, null, null),
                Times.Once());
            Assert.That(context.Response.Headers.Location.ToString(), Is.EqualTo(RefusedCredentials.LoginPath));
        }

        [Test]
        public async Task Logout_Should_IgnoreAnUnknownReason()
        {
            var context = CreateContext("/logout", "?reason=https://example.com");

            await CreateMiddleware().Invoke(context);

            Assert.That(context.Response.Headers.Location.ToString(), Is.EqualTo("/"));
        }
    }
}
