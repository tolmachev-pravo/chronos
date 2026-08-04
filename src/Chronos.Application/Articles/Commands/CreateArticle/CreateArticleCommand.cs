using MediatR;
using System;

namespace Chronos.Application.Articles.Commands.CreateArticle
{
    public class CreateArticleCommand : IRequest<Guid>
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
    }
}
