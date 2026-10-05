using System;

namespace Chronos.Domain.Entities.Calendar
{
	/// <summary>
	/// A stretch of days the user is away: a vacation is one row from its first day to its
	/// last, not a row per day. See issue #310.
	/// </summary>
	public class UserAbsence : BaseEntity
	{
		public string Username { get; set; }

		/// <summary>The first day away, inclusive.</summary>
		public DateTime StartDate { get; set; }

		/// <summary>The last day away, inclusive.</summary>
		public DateTime EndDate { get; set; }

		public AbsenceKind Kind { get; set; }

		public string Comment { get; set; }
	}
}
