using MediatR;
using NUnit.Framework;
using Chronos.Application.Authentication;
using Chronos.Application.Common.Behaviors;
using System;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.UnitTests.Application.Common
{
    /// <summary>
    /// The one place a refusal by Jira is recognised, so that a scenario failing on a 401
    /// is told from a scenario failing on anything else. See issue #305.
    /// </summary>
    [TestFixture]
    public class AuthenticationBehaviorTests
    {
        private class Request : IRequest<string>
        {
        }

        private static Task<string> Handle(Exception? thrown)
        {
            var behavior = new AuthenticationBehavior<Request, string>();
            return behavior.Handle(
                new Request(),
                () => thrown is null
                    ? Task.FromResult("done")
                    : Task.FromException<string>(thrown),
                CancellationToken.None);
        }

        [Test]
        public async Task Handle_Should_ReturnTheAnswer_When_NothingFailed()
        {
            Assert.That(await Handle(thrown: null), Is.EqualTo("done"));
        }

        [Test]
        public void Handle_Should_TellARefusalByJira()
        {
            var refusal = new AuthenticationException("401");

            var exception = Assert.ThrowsAsync<JiraAuthenticationException>(() => Handle(refusal));

            Assert.That(exception.InnerException, Is.SameAs(refusal));
        }

        [Test]
        public void Handle_Should_TellARefusalWrappedByWhateverAwaitedIt()
        {
            var wrapped = new InvalidOperationException("read failed", new AuthenticationException("401"));

            Assert.ThrowsAsync<JiraAuthenticationException>(() => Handle(wrapped));
        }

        [Test]
        public void Handle_Should_LeaveEveryOtherFailureAlone()
        {
            var failure = new InvalidOperationException("jira is down");

            Assert.That(
                Assert.ThrowsAsync<InvalidOperationException>(() => Handle(failure)),
                Is.SameAs(failure));
        }

        [Test]
        public void Handle_Should_NotDescribeTheSameRefusalTwice()
        {
            // A scenario built from other scenarios passes through the behavior once per
            // level; the refusal must not gain a wrapper each time.
            var alreadyTold = new JiraAuthenticationException(new AuthenticationException("401"));

            Assert.That(
                Assert.ThrowsAsync<JiraAuthenticationException>(() => Handle(alreadyTold)),
                Is.SameAs(alreadyTold));
        }
    }
}
