using Pet.Jira.Domain.Entities.Blog;
using System;
using System.Linq.Expressions;

namespace Pet.Jira.Application.Articles.Dto
{
    public class ArticleDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Projection from <see cref="Article"/>, translatable by EF Core
        /// so that only the required columns are selected.
        /// </summary>
        public static Expression<Func<Article, ArticleDto>> Projection { get; } =
            article => new ArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                Content = article.Content,
                ImageUrl = article.ImageUrl,
                Link = article.Link,
                CreatedAt = article.CreatedAt
            };
    }
}
