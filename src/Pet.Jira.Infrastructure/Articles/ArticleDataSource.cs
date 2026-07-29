using Microsoft.EntityFrameworkCore;
using Pet.Jira.Application.Articles;
using Pet.Jira.Application.Articles.Dto;
using Pet.Jira.Infrastructure.Data.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Infrastructure.Articles
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
