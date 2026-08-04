using Microsoft.AspNetCore.Components;
using Chronos.Web.Shared;
using System;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Markdown
{
    public partial class ComponentMarkdownViewer : ComponentBase
    {
        [Parameter] public string MarkdownPath { get; set; }
        [Inject] private IMarkdownService MarkdownService { get; init; } = default!;
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }

        private string Value { get; set; } = string.Empty;
        
        protected override async Task OnInitializedAsync()
        {
            try
            {
                Value = await MarkdownService.DownloadMarkdownAsync(MarkdownPath);
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }            
        }

    }
}
