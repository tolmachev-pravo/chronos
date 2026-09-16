namespace Chronos.Domain.Models.Events
{
    /// <summary>
    /// The stretch of time an issue spent in one status, and the statuses on either side
    /// of it. See issue #156.
    /// </summary>
    public class TransitionEventDetails : IEventDetails
    {
        /// <summary>
        /// The status the event is time in — In Progress for work, the testing status for
        /// tester events.
        /// </summary>
        public string Status { get; init; }

        /// <summary>
        /// Where the issue came from. Null when entering the status predates the changelog
        /// the period asked for.
        /// </summary>
        public string FromStatus { get; init; }

        /// <summary>
        /// Where the issue went. Null when it had not left the status yet.
        /// </summary>
        public string ToStatus { get; init; }
    }
}
