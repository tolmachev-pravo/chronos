namespace Chronos.Domain.Models.Events
{
    /// <summary>
    /// Where an event came from. Replaces WorklogSource, which described events all
    /// along — a real time entry has no source of this kind. See issue #299.
    /// </summary>
    public enum EventSource
    {
        Assignee,
        Comment,
        Calendar,
        Tester
    }
}
