using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Chronos.Web.Components.Markdown
{
    public partial class MarkdownTooltip : ComponentBase
    {
        /// <summary>
        /// Child content of component.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Component markdown name.
        /// </summary>
        [Parameter] 
        public string Markdown { get; set; }

        /// <summary>
        /// Component custom class names, separated by space.
        /// </summary>
        [Parameter]
        public string RootClass { get; set; }

        /// <summary>
        /// Tooltip color
        /// </summary>
        [Parameter] 
        public Color Color { get; set; } = Color.Primary;

        private string MarkdownPath => $"wwwroot/documents/components/{Markdown}.md";
    }
}
