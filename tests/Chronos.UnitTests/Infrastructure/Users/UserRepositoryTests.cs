using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Chronos.Infrastructure.Data.Contexts;
using Chronos.Infrastructure.Users;

namespace Chronos.UnitTests.Infrastructure.Users
{
    [TestFixture]
    public class UserRepositoryTests
    {
        private SqliteConnection _connection;
        private DbContextOptions<ApplicationDbContext> _options;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new ApplicationDbContext(_options);
            context.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            _connection.Dispose();
        }

        [Test]
        public async Task EnsureUserExistsAsync_Should_CreateUser_WhenNotExists()
        {
            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new UserRepository(context, NullLogger<UserRepository>.Instance);
                await repository.EnsureUserExistsAsync("john");
            }

            using var assertContext = new ApplicationDbContext(_options);
            Assert.That(assertContext.Users.Count(user => user.Username == "john"), Is.EqualTo(1));
        }

        [Test]
        public async Task EnsureUserExistsAsync_Should_NotCreateDuplicate_WhenCalledTwice()
        {
            using (var context = new ApplicationDbContext(_options))
            {
                await new UserRepository(context, NullLogger<UserRepository>.Instance).EnsureUserExistsAsync("john");
            }
            using (var context = new ApplicationDbContext(_options))
            {
                await new UserRepository(context, NullLogger<UserRepository>.Instance).EnsureUserExistsAsync("john");
            }

            using var assertContext = new ApplicationDbContext(_options);
            Assert.That(assertContext.Users.Count(user => user.Username == "john"), Is.EqualTo(1));
        }
    }
}
