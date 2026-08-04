namespace Chronos.Domain.Entities.Blog
{
	public class Article : BaseEntity
	{
		public string Title { get; set; }
		public string Content { get; set; }
		public string ImageUrl { get; set; }
		public string Link { get; set; }
	}
}
