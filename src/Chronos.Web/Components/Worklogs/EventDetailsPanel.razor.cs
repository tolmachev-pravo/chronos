using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Domain.Models.Events;

namespace Chronos.Web.Components.Worklogs
{
    /// <summary>
    /// What the source knew about the event a suggestion came from, shown under its row.
    /// The labels are Russian; the statuses, the comment and the meeting fields arrive from
    /// Jira and the calendar and are shown as they came. See issue #156.
    /// </summary>
    public partial class EventDetailsPanel : ComponentBase
    {
        [Parameter] public IEventDetails Details { get; set; }

        /// <summary>
        /// The colour of the row's source, which the panel borrows for its edge and its
        /// link so the two read as one thing.
        /// </summary>
        [Parameter] public Color Color { get; set; } = Color.Primary;

        private string AccentStyle =>
            $"--chr-event-accent: var(--mud-palette-{Color.ToString().ToLowerInvariant()})";

        private static string CommentLabel(CommentEventDetails comment) =>
            string.IsNullOrEmpty(comment.Author)
                ? "Комментарий"
                : $"Комментарий · {comment.Author}";
    }
}
