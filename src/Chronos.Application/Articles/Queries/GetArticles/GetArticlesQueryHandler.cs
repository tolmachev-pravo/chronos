using MediatR;
using Chronos.Application.Articles.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Articles.Queries.GetArticles
{
	public class GetArticlesQueryHandler : IRequestHandler<GetArticlesQuery, IEnumerable<ArticleDto>>
	{
		private readonly IArticleDataSource _dataSource;

		public GetArticlesQueryHandler(
			IArticleDataSource dataSource)
		{
			_dataSource = dataSource;
		}

		public Task<IEnumerable<ArticleDto>> Handle(
			GetArticlesQuery request,
			CancellationToken cancellationToken)
		{
			return _dataSource.GetArticlesAsync(cancellationToken);
		}
	}
}
