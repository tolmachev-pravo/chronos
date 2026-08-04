using Microsoft.AspNetCore.Components;
using Chronos.Application.Articles.Dto;

namespace Chronos.Web.Components.Project.Articles
{
    public partial class Article : ComponentBase
    {
        [Parameter] public ArticleDto Entity { get; set; }
    }
}
