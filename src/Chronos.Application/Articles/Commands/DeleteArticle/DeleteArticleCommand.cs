using MediatR;
using System;

namespace Chronos.Application.Articles.Commands.DeleteArticle
{
    public class DeleteArticleCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
