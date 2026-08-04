using Chronos.Application.Articles.Commands.CreateArticle;
using Chronos.Domain.Entities.Blog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Articles
{
    public interface IArticleRepository
    {
        Task<Article> AddAsync(CreateArticleCommand article, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
