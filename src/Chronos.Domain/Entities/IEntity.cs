using System;

namespace Chronos.Domain.Entities
{
	public interface IEntity
	{
		Guid Id { get; set; }
	}
}
