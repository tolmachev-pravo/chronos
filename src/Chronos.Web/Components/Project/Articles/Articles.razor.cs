using MediatR;
using Microsoft.AspNetCore.Components;
using Chronos.Application.Articles.Dto;
using Chronos.Application.Articles.Queries.GetArticles;
using Chronos.Web.Components.Common;
using Chronos.Web.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Project.Articles
{
    public partial class Articles : ComponentBase
    {
        private readonly ComponentModel _model = ComponentModel.Create();

        [Inject] private IMediator Mediator { get; set; }
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var entities = await Mediator.Send(new GetArticlesQuery());
            _model.Entities = entities.OrderByDescending(entity => entity.CreatedAt);
        }

        private class ComponentModel : BaseStateComponentModel
        {
            public static ComponentModel Create()
            {
                return new ComponentModel();
            }

            public IEnumerable<ArticleDto> Entities { get; set; }
        }
    }
}
