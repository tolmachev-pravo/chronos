using Moq;
using Chronos.Application.Storage;
using Chronos.Domain.Models.Abstract;
using Chronos.Infrastructure.Storage;

namespace Chronos.UnitTests.Infrastructure.Storage
{
    [TestFixture]
    public class BaseStorageTests
    {
        public class Entry : IEntity<string>
        {
            public string Key { get; set; } = string.Empty;
        }

        public class EntryStorage : BaseStorage<string, Entry>
        {
            public EntryStorage(
                ILocalStorage<Entry> localStorage,
                IMemoryCache<string, Entry> memoryCache,
                IDataSource<string, Entry> dataSource)
                : base(localStorage, memoryCache, dataSource)
            {
            }
        }

        [Test]
        public async Task InitAsync_Should_DoNothing_WhenThereIsNoSourceToInitialiseFrom()
        {
            // A storage may be built without a data source — the mock user profile is one —
            // and outside a browser the local tier answers nothing. Neither is a failure.
            var localStorage = new Mock<ILocalStorage<Entry>>();
            localStorage
                .Setup(storage => storage.GetValueAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entry)null!);
            var memoryCache = new Mock<IMemoryCache<string, Entry>>();
            var storage = new EntryStorage(localStorage.Object, memoryCache.Object, dataSource: null!);

            Assert.DoesNotThrowAsync(() => storage.InitAsync("john"));
            await Task.CompletedTask;
        }

        [Test]
        public async Task InitAsync_Should_FallBackToTheDataSource_WhenTheLocalTierHasNothing()
        {
            var localStorage = new Mock<ILocalStorage<Entry>>();
            localStorage
                .Setup(storage => storage.GetValueAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entry)null!);
            var memoryCache = new Mock<IMemoryCache<string, Entry>>();
            var dataSource = new Mock<IDataSource<string, Entry>>();
            dataSource
                .Setup(source => source.GetAsync("john", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Entry { Key = "john" });
            var storage = new EntryStorage(localStorage.Object, memoryCache.Object, dataSource.Object);

            await storage.InitAsync("john");

            memoryCache.Verify(
                cache => cache.TryUpdate("john", It.Is<Entry>(entry => entry.Key == "john")),
                Times.Once());
        }
    }
}
