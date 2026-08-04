using MediatR;
using Chronos.Application.Articles.Dto;
using System.Collections.Generic;

namespace Chronos.Application.Articles.Queries.GetArticles
{
    public class GetArticlesQuery : IRequest<IEnumerable<ArticleDto>>
    {
    }
}