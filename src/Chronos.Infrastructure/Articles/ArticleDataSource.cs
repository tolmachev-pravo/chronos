using Microsoft.EntityFrameworkCore;
using Chronos.Application.Articles;
using Chronos.Application.Articles.Dto;
using Chronos.Infrastructure.Data.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Articles
{
    public class ArticleDataSource : IArticleDataSource
    {
        private readonly ApplicationDbContext _dbContext;

        public ArticleDataSource(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<ArticleDto>> GetArticlesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Articles
                .Select(ArticleDto.Projection)
                .ToListAsync(cancellationToken);
        }
    }
}
