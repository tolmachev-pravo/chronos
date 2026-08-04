using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Articles.Commands.DeleteArticle
{
    public class DeleteArticleCommandHandler : IRequestHandler<DeleteArticleCommand, bool>
    {
        private readonly IArticleRepository _repository;

        public DeleteArticleCommandHandler(
            IArticleRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteArticleCommand request, CancellationToken cancellationToken)
        {
            return await _repository.DeleteAsync(request.Id, cancellationToken);
        }
    }
}
