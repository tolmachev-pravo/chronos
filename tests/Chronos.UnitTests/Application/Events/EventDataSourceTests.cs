using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Chronos.Application.Events;
using Chronos.Application.Tracing;
using Chronos.Domain.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.UnitTests.Application.Events
{
    /// <summary>
    /// The orchestrator owns what used to be spread over GetWorklogCollection: skipping
    /// disabled sources, degrading on a failing one, and keeping the settings reads out
    /// of the parallel phase. See issue #299.
    /// </summary>
    [TestFixture]
    public class EventDataSourceTests
    {
        private Moq.Mock<IPerformanceStatsCollector> _statsMock;

        [SetUp]
        public void Setup()
        {
            _statsMock = new Moq.Mock<IPerformanceStatsCollector>();
        }

        private static EventQuery Query() => new(
            "user1",
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 7));

        private EventDataSource CreateSut(params IEventProvider[] providers) => new(
            providers,
            _statsMock.Object,
            Moq.Mock.Of<ILogger<EventDataSource>>());

        private static UserEvent Event(EventSource source) => new()
        {
            StartDate = new DateTime(2026, 6, 1, 10, 0, 0),
            CompleteDate = new DateTime(2026, 6, 1, 11, 0, 0),
            Author = "user1",
            Source = source
        };

        private static Moq.Mock<IEventProvider> Provider(
            EventSource source,
            bool prepared = true,
            IEnumerable<IEvent>? events = null)
        {
            var mock = new Moq.Mock<IEventProvider>();
            mock.SetupGet(provider => provider.Source).Returns(source);
            mock.Setup(provider => provider.PrepareAsync(It.IsAny<EventQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(prepared);
            mock.Setup(provider => provider.GetEventsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(events ?? new List<IEvent> { Event(source) });
            return mock;
        }

        [Test]
        public async Task GetEventsAsync_Should_ConcatenateTheEventsOfEveryPreparedProvider()
        {
            var assignee = Provider(EventSource.Assignee);
            var calendar = Provider(EventSource.Calendar);

            var result = await CreateSut(assignee.Object, calendar.Object).GetEventsAsync(Query());

            Assert.That(result.Select(userEvent => userEvent.Source),
                Is.EquivalentTo(new[] { EventSource.Assignee, EventSource.Calendar }));
        }

        [Test]
        public async Task GetEventsAsync_Should_NotFetch_When_TheProviderIsNotPrepared()
        {
            var disabled = Provider(EventSource.Comment, prepared: false);
            var enabled = Provider(EventSource.Assignee);

            var result = await CreateSut(disabled.Object, enabled.Object).GetEventsAsync(Query());

            // A disabled source is not queried at all — the external system stays untouched.
            // See issue #242.
            disabled.Verify(provider => provider.GetEventsAsync(It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(result.Single().Source, Is.EqualTo(EventSource.Assignee));
        }

        [Test]
        public async Task GetEventsAsync_Should_SkipAFailingProvider_And_KeepTheRest()
        {
            var failing = Provider(EventSource.Calendar);
            failing.Setup(provider => provider.GetEventsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("calendar down"));
            var healthy = Provider(EventSource.Assignee);

            var result = await CreateSut(failing.Object, healthy.Object).GetEventsAsync(Query());

            Assert.That(result.Single().Source, Is.EqualTo(EventSource.Assignee));
        }

        [Test]
        public void GetEventsAsync_Should_NotSkipAProvider_When_JiraRefusedTheUser()
        {
            // A 401 empties every Jira source at once, so a day assembled from what is
            // left would be wrong without saying so. It reaches the caller. See issue #305.
            var refused = Provider(EventSource.Tester);
            refused.Setup(provider => provider.GetEventsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AuthenticationException("401"));
            var healthy = Provider(EventSource.Assignee);

            Assert.ThrowsAsync<AuthenticationException>(
                () => CreateSut(refused.Object, healthy.Object).GetEventsAsync(Query()));
        }

        [Test]
        public void PrepareAsync_Should_NotSkipAProvider_When_JiraRefusedTheUser()
        {
            var refused = Provider(EventSource.Tester);
            refused.Setup(provider => provider.PrepareAsync(It.IsAny<EventQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AuthenticationException("401"));

            Assert.ThrowsAsync<AuthenticationException>(
                () => CreateSut(refused.Object).GetEventsAsync(Query()));
        }

        [Test]
        public async Task GetEventsAsync_Should_SkipAProviderThatFailsToPrepare()
        {
            var failing = Provider(EventSource.Comment);
            failing.Setup(provider => provider.PrepareAsync(It.IsAny<EventQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("settings unreadable"));
            var healthy = Provider(EventSource.Assignee);

            var result = await CreateSut(failing.Object, healthy.Object).GetEventsAsync(Query());

            failing.Verify(provider => provider.GetEventsAsync(It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(result.Single().Source, Is.EqualTo(EventSource.Assignee));
        }

        [Test]
        public async Task GetEventsAsync_Should_FinishEveryPrepare_BeforeTheFirstFetch()
        {
            // The prepare phase reads settings through the scoped, non-thread-safe
            // DbContext, so it must not overlap the parallel fetch. See issue #258.
            var preparesDone = 0;
            var fetchStartedTooEarly = false;

            var providers = new[] { EventSource.Assignee, EventSource.Comment, EventSource.Calendar }
                .Select(source =>
                {
                    var mock = new Moq.Mock<IEventProvider>();
                    mock.SetupGet(provider => provider.Source).Returns(source);
                    mock.Setup(provider => provider.PrepareAsync(It.IsAny<EventQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(() =>
                        {
                            Interlocked.Increment(ref preparesDone);
                            return true;
                        });
                    mock.Setup(provider => provider.GetEventsAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(() =>
                        {
                            if (Volatile.Read(ref preparesDone) < 3)
                            {
                                fetchStartedTooEarly = true;
                            }
                            return new List<IEvent> { Event(source) };
                        });
                    return mock.Object;
                })
                .ToArray();

            await CreateSut(providers).GetEventsAsync(Query());

            Assert.That(fetchStartedTooEarly, Is.False);
        }

        [Test]
        public async Task GetEventsAsync_Should_RecordAMeasurePerProvider()
        {
            var assignee = Provider(EventSource.Assignee);
            var calendar = Provider(EventSource.Calendar);

            await CreateSut(assignee.Object, calendar.Object).GetEventsAsync(Query());

            // The per-source breakdown separate MediatR requests used to give. See issue #258.
            _statsMock.Verify(
                stats => stats.Record(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<long>()),
                Times.Exactly(2));
        }
    }
}
