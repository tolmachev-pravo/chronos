using System.Threading.Tasks;

namespace Chronos.Web.Components.Markdown
{
	public interface IMarkdownService
	{
		Task<string> DownloadMarkdownAsync(string path);
	}
}
