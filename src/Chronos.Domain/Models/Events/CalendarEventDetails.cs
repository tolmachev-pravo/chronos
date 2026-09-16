using System.Collections.Generic;

namespace Chronos.Domain.Models.Events
{
    /// <summary>
    /// The meeting an event stands for. See issue #156.
    /// </summary>
    public class CalendarEventDetails : IEventDetails
    {
        /// <summary>
        /// The meeting's own title. The row shows this rather than the summary of the
        /// issue its key was found in — the meeting is what happened.
        /// </summary>
        public string Title { get; init; }

        public string Description { get; init; }

        /// <summary>
        /// Who called the meeting, by display name where the invitation carried one.
        /// </summary>
        public string Organizer { get; init; }

        public string Location { get; init; }

        /// <summary>
        /// Everyone invited. Empty rather than null when the invitation named nobody.
        /// </summary>
        public IReadOnlyList<string> Attendees { get; init; } = new List<string>();
    }
}
