using System;

namespace Chronos.Domain.Entities.Calendar
{
	/// <summary>
	/// One exception to the weekday rule in the production calendar, shared by every user.
	/// Only the exceptions are stored, not each day of the year: a year nobody loaded keeps
	/// working by the weekday rule. See issue #310.
	/// </summary>
	public class CalendarDay : BaseEntity
	{
		/// <summary>The day, without time. Unique.</summary>
		public DateTime Date { get; set; }

		public CalendarDayKind Kind { get; set; }

		/// <summary>«День России», «Перенесённый выходной»…</summary>
		public string Title { get; set; }
	}
}
