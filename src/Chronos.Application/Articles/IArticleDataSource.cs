using Chronos.Application.Articles.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Articles
{
    public interface IArticleDataSource
    {
        Task<IEnumerable<ArticleDto>> GetArticlesAsync(CancellationToken cancellationToken = default);
    }
}
