namespace Chronos.Domain.Entities.Calendar
{
	/// <summary>
	/// How a day of the production calendar departs from the rule «Monday to Friday are
	/// working, Saturday and Sunday are not». See issue #310.
	/// </summary>
	public enum CalendarDayKind
	{
		/// <summary>A day off: a public holiday, or a day off carried over onto a weekday.</summary>
		Holiday,

		/// <summary>A working day that falls on a weekend — a working Saturday.</summary>
		Workday,

		/// <summary>A working day before a holiday, an hour shorter than usual.</summary>
		ShortDay
	}
}
