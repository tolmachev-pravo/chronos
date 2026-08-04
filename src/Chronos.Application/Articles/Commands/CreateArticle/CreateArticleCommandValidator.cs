using FluentValidation;

namespace Chronos.Application.Articles.Commands.CreateArticle
{
	public class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
	{
		public CreateArticleCommandValidator()
		{
			RuleFor(entity => entity.Title)
				.NotEmpty()
				.MaximumLength(100);

			RuleFor(entity => entity.Content)
				.NotEmpty()
				.MaximumLength(1000);

			RuleFor(entity => entity.ImageUrl)
				.NotEmpty()
				.MaximumLength(1000);

			RuleFor(entity => entity.Link)
				.NotEmpty()
				.MaximumLength(1000);
		}
	}
}
