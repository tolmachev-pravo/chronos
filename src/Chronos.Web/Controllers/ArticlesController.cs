using MediatR;
using Microsoft.AspNetCore.Mvc;
using Chronos.Application.Articles.Commands.CreateArticle;
using Chronos.Application.Articles.Commands.DeleteArticle;
using Chronos.Application.Articles.Queries.GetArticles;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ArticlesController(
            IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<object>> Create(
            CreateArticleCommand article,
            CancellationToken cancellationToken = default)
        {
            var articleId = await _mediator.Send(article, cancellationToken);
            var articles = await _mediator.Send(new GetArticlesQuery(), cancellationToken);
            return articles.First(entity => entity.Id == articleId);
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll(
            CancellationToken cancellationToken = default)
        {
            var articles = await _mediator.Send(new GetArticlesQuery(), cancellationToken);
            return Ok(articles);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var deleted = await _mediator.Send(new DeleteArticleCommand { Id = id }, cancellationToken);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
