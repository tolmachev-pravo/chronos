using Moq;
using Chronos.Application.Authentication;
using Chronos.Domain.Models.Users;
using Chronos.Web.Authentication;

namespace Chronos.UnitTests.Web.Authentication
{
    [TestFixture]
    public class CompositeIdentityServiceTests
    {
        private RequestUserAccessor _requestUserAccessor = null!;
        private Mock<IIdentityService> _circuitIdentityService = null!;
        private int _circuitResolutions;

        [SetUp]
        public void SetUp()
        {
            _requestUserAccessor = new RequestUserAccessor();
            _circuitIdentityService = new Mock<IIdentityService>();
            _circuitResolutions = 0;
        }

        private CompositeIdentityService CreateService()
        {
            return new CompositeIdentityService(
                _requestUserAccessor,
                () =>
                {
                    _circuitResolutions++;
                    return _circuitIdentityService.Object;
                });
        }

        [Test]
        public async Task GetCurrentUserAsync_Should_ReturnRequestUser_WhenRequestIsAuthenticatedByToken()
        {
            _requestUserAccessor.User = new User { Username = "john", PersonalAccessToken = "token" };

            var user = await CreateService().GetCurrentUserAsync();

            Assert.That(user!.Username, Is.EqualTo("john"));
            Assert.That(user.PersonalAccessToken, Is.EqualTo("token"));
        }

        [Test]
        public async Task GetCurrentUserAsync_Should_NotTouchTheCircuit_WhenRequestCarriesItsOwnUser()
        {
            // Asking a circuit for its state outside a circuit throws, so the token path
            // must not even resolve the service.
            _requestUserAccessor.User = new User { Username = "john" };

            await CreateService().GetCurrentUserAsync();

            Assert.That(_circuitResolutions, Is.Zero);
        }

        [Test]
        public async Task GetCurrentUserAsync_Should_FallBackToTheCircuit_WhenRequestHasNoUser()
        {
            _circuitIdentityService
                .Setup(service => service.GetCurrentUserAsync())
                .ReturnsAsync(new User { Username = "circuit-john" });

            var user = await CreateService().GetCurrentUserAsync();

            Assert.That(user!.Username, Is.EqualTo("circuit-john"));
        }

        [Test]
        public void CurrentUser_Should_FollowTheSameOrder_AsTheAsyncResolution()
        {
            _circuitIdentityService
                .Setup(service => service.CurrentUser)
                .Returns(new User { Username = "circuit-john" });
            var service = CreateService();

            Assert.That(service.CurrentUser!.Username, Is.EqualTo("circuit-john"));

            _requestUserAccessor.User = new User { Username = "token-john" };

            Assert.That(service.CurrentUser!.Username, Is.EqualTo("token-john"));
        }
    }
}
