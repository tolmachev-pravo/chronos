using System;

namespace Chronos.Domain.Entities.Users
{
	/// <summary>
	/// System settings of a user that used to be asked for in the worklog filter
	/// (issue #241): when the working day starts and ends and how long lunch takes.
	/// One row per user; a user without a row works with the defaults.
	/// </summary>
	public class UserSettings : BaseEntity
	{
		public string Username { get; set; }
		public TimeSpan WorkingStartTime { get; set; }
		public TimeSpan WorkingEndTime { get; set; }
		public TimeSpan LunchTime { get; set; }
	}
}
