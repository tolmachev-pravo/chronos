using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Chronos.Domain.Entities.Blog;
using Chronos.Infrastructure.Articles;
using Chronos.Infrastructure.Data.Contexts;

namespace Chronos.UnitTests.Infrastructure.Articles
{
    [TestFixture]
    public class ArticleDataSourceTests
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
        public async Task GetArticlesAsync_Should_ProjectAllFields()
        {
            var createdAt = new DateTime(2026, 7, 29, 10, 15, 0, DateTimeKind.Utc);
            using (var context = new ApplicationDbContext(_options))
            {
                context.Articles.Add(new Article
                {
                    Id = Guid.NewGuid(),
                    Title = "Title",
                    Content = "Content",
                    ImageUrl = "https://example.org/image.png",
                    Link = "https://example.org/article",
                    CreatedAt = createdAt
                });
                await context.SaveChangesAsync();
            }

            using var queryContext = new ApplicationDbContext(_options);
            var articles = (await new ArticleDataSource(queryContext).GetArticlesAsync()).ToList();

            Assert.That(articles, Has.Count.EqualTo(1));
            var article = articles.Single();
            Assert.Multiple(() =>
            {
                Assert.That(article.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(article.Title, Is.EqualTo("Title"));
                Assert.That(article.Content, Is.EqualTo("Content"));
                Assert.That(article.ImageUrl, Is.EqualTo("https://example.org/image.png"));
                Assert.That(article.Link, Is.EqualTo("https://example.org/article"));
                Assert.That(article.CreatedAt, Is.EqualTo(createdAt));
            });
        }

        [Test]
        public async Task GetArticlesAsync_Should_ReturnEmpty_WhenNoArticles()
        {
            using var context = new ApplicationDbContext(_options);

            var articles = await new ArticleDataSource(context).GetArticlesAsync();

            Assert.That(articles, Is.Empty);
        }
    }
}
