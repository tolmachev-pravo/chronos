using Chronos.Domain.Entities.Users;
using System;

namespace Chronos.Domain.Entities.Notifications
{
	public class UserNotification : BaseEntity
	{
		public User User { get; set; }
		public Guid UserId { get; set; }

		public Guid NotificationEntityId { get; set; }
		public NotificationEntity NotificationEntity { get; set; }

		public bool IsRead { get; set; }
		public DateTime? ReadAt { get; set; }

		public string Message { get; set; }
	}
}
