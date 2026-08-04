using Chronos.Application.Articles;
using Chronos.Application.Articles.Commands.CreateArticle;
using Chronos.Domain.Entities.Blog;
using Chronos.Infrastructure.Data.Contexts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Articles
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ArticleRepository(
            ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Article> AddAsync(CreateArticleCommand article, CancellationToken cancellationToken = default)
        {
            var articleEntity = new Article
            {
                Title = article.Title,
                Content = article.Content,
                ImageUrl = article.ImageUrl,
                Link = article.Link,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Articles.Add(articleEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return articleEntity;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.Articles.FindAsync(new object?[] { id }, cancellationToken);
            if (entity == null)
                return false;

            _dbContext.Articles.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
