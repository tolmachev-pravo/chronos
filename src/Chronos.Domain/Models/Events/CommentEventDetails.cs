namespace Chronos.Domain.Models.Events
{
    /// <summary>
    /// The comment an event stands for. See issue #156.
    /// </summary>
    public class CommentEventDetails : IEventDetails
    {
        /// <summary>
        /// The comment as Jira stores it — wiki markup, not rendered HTML.
        /// </summary>
        public string Body { get; init; }

        /// <summary>
        /// A link that opens the issue with this comment in view.
        /// </summary>
        public string Link { get; init; }

        /// <summary>
        /// Who wrote it, by display name. May be null when Jira answered without the
        /// author's profile, in which case the username stands in.
        /// </summary>
        public string Author { get; init; }
    }
}
