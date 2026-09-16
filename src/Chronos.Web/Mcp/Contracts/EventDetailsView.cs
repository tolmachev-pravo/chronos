namespace Chronos.Web.Mcp.Contracts
{
    /// <summary>
    /// What the source of an event knew about it beyond its time and its issue. One flat
    /// shape for all sources rather than one per source: which fields are filled says which
    /// kind of event it was, and a client reading this does not have to know the kinds.
    /// Every field may be absent. See issue #156.
    /// </summary>
    /// <param name="Text">
    /// The words of the event: the comment the user left, or the description of the meeting.
    /// Jira comments arrive as wiki markup, not HTML or Markdown.
    /// </param>
    /// <param name="Link">A link that opens the comment in Jira.</param>
    /// <param name="From">
    /// The status the issue was in before this stretch of time. Absent when it entered the
    /// status before the period being asked about.
    /// </param>
    /// <param name="Status">The status the issue sat in for the length of the event.</param>
    /// <param name="To">
    /// The status the issue moved on to. Absent when it had not moved on yet — the work is
    /// still open.
    /// </param>
    /// <param name="Title">
    /// What the meeting called itself, which is not the summary of the issue its key was
    /// found in.
    /// </param>
    /// <param name="Organizer">Who called the meeting.</param>
    /// <param name="Location">Where it was held — a room, a link, or both.</param>
    /// <param name="Attendees">Everyone invited. Absent for anything but a meeting.</param>
    public record EventDetailsView(
        string Text = null,
        string Link = null,
        string From = null,
        string Status = null,
        string To = null,
        string Title = null,
        string Organizer = null,
        string Location = null,
        System.Collections.Generic.IReadOnlyList<string> Attendees = null);
}
