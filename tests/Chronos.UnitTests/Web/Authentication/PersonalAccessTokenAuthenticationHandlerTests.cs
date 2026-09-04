using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Chronos.Application.Authentication;
using Chronos.Web.Authentication;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ChronosAuthenticationService = Chronos.Application.Authentication.IAuthenticationService;

namespace Chronos.UnitTests.Web.Authentication
{
    [TestFixture]
    public class PersonalAccessTokenAuthenticationHandlerTests
    {
        private Mock<ChronosAuthenticationService> _authenticationService = null!;
        private RequestUserAccessor _requestUserAccessor = null!;
        private IMemoryCache _validatedTokens = null!;

        [SetUp]
        public void SetUp()
        {
            _authenticationService = new Mock<ChronosAuthenticationService>();
            _authenticationService
                .Setup(service => service.LoginAsync(It.IsAny<BearerLoginRequest>()))
                .ReturnsAsync(new LoginResponse(true) { Username = "john" });
            _requestUserAccessor = new RequestUserAccessor();
            _validatedTokens = new MemoryCache(new MemoryCacheOptions());
        }

        [TearDown]
        public void TearDown()
        {
            _validatedTokens.Dispose();
        }

        private async Task<AuthenticateResult> AuthenticateAsync(string? authorizationHeader)
        {
            var services = new Mock<IServiceProvider>();
            services
                .Setup(provider => provider.GetService(typeof(ChronosAuthenticationService)))
                .Returns(_authenticationService.Object);

            var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
            options
                .Setup(monitor => monitor.Get(It.IsAny<string>()))
                .Returns(new AuthenticationSchemeOptions());

            var handler = new PersonalAccessTokenAuthenticationHandler(
                options.Object,
                NullLoggerFactory.Instance,
                UrlEncoder.Default,
                services.Object,
                _requestUserAccessor,
                _validatedTokens);

            var context = new DefaultHttpContext();
            if (authorizationHeader is not null)
            {
                context.Request.Headers.Authorization = authorizationHeader;
            }

            await handler.InitializeAsync(
                new AuthenticationScheme(
                    PersonalAccessTokenDefaults.AuthenticationScheme,
                    displayName: null,
                    handlerType: typeof(PersonalAccessTokenAuthenticationHandler)),
                context);

            return await handler.AuthenticateAsync();
        }

        [Test]
        public async Task AuthenticateAsync_Should_PassTheRequestOn_WhenThereIsNoBearerToken()
        {
            var withoutHeader = await AuthenticateAsync(null);
            var withAnotherScheme = await AuthenticateAsync("Basic am9objpzZWNyZXQ=");

            Assert.That(withoutHeader.None, Is.True);
            Assert.That(withAnotherScheme.None, Is.True);
            _authenticationService.Verify(
                service => service.LoginAsync(It.IsAny<BearerLoginRequest>()),
                Times.Never());
        }

        [Test]
        public async Task AuthenticateAsync_Should_Fail_WhenTheTokenIsEmpty()
        {
            var result = await AuthenticateAsync("Bearer   ");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.None, Is.False);
        }

        [Test]
        public async Task AuthenticateAsync_Should_SignInAsTheTokenOwner_AsJiraKnowsThem()
        {
            var result = await AuthenticateAsync("Bearer pat-123");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Principal!.Identity!.Name, Is.EqualTo("john"));
            Assert.That(
                result.Principal.FindFirstValue(ClaimTypes.Name),
                Is.EqualTo("john"));
        }

        [Test]
        public async Task AuthenticateAsync_Should_HandTheCredentialsToTheRequest_SoScenariosCanReachJira()
        {
            await AuthenticateAsync("Bearer pat-123");

            Assert.That(_requestUserAccessor.User, Is.Not.Null);
            Assert.That(_requestUserAccessor.User!.Username, Is.EqualTo("john"));
            Assert.That(_requestUserAccessor.User.PersonalAccessToken, Is.EqualTo("pat-123"));
        }

        [Test]
        public async Task AuthenticateAsync_Should_Fail_AndLeaveNoUserBehind_WhenJiraRejectsTheToken()
        {
            _authenticationService
                .Setup(service => service.LoginAsync(It.IsAny<BearerLoginRequest>()))
                .ThrowsAsync(new Exception("401 Unauthorized"));

            var result = await AuthenticateAsync("Bearer revoked");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(_requestUserAccessor.User, Is.Null);
        }

        [Test]
        public async Task AuthenticateAsync_Should_AskJiraOncePerToken_WhileTheValidationIsRemembered()
        {
            await AuthenticateAsync("Bearer pat-123");
            await AuthenticateAsync("Bearer pat-123");
            await AuthenticateAsync("Bearer pat-456");

            _authenticationService.Verify(
                service => service.LoginAsync(It.IsAny<BearerLoginRequest>()),
                Times.Exactly(2));
        }
    }
}
