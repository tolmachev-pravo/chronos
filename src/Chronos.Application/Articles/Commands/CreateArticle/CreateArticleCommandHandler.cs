using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Articles.Commands.CreateArticle
{
	public class CreateArticleCommandHandler : IRequestHandler<CreateArticleCommand, Guid>
	{
		private readonly IArticleRepository _repository;

		public CreateArticleCommandHandler(
			IArticleRepository repository)
		{
			_repository = repository;
		}

		public async Task<Guid> Handle(CreateArticleCommand request, CancellationToken cancellationToken)
		{
			var entity = await _repository.AddAsync(request, cancellationToken);
			return entity.Id;
		}
	}
}
