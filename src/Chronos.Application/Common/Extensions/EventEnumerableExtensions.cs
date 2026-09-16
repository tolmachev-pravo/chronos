using Chronos.Domain.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chronos.Application.Common.Extensions
{
	internal static class EventEnumerableExtensions
	{
		/// <summary>
		/// Clips every event to the days it spans, so a multi-day event contributes one
		/// piece per day. Was SplitByDays over IWorklog before events became their own
		/// concept. See issue #299.
		/// </summary>
		public static IEnumerable<IEvent> SplitByDays(
			this IEnumerable<IEvent> events,
			DateTime firstDate,
			DateTime lastDate)
		{
			var day = lastDate.Date;
			while (day >= firstDate.Date)
			{
				var startOfDay = day;
				var endOfDay = day.EndOfDay();

				var dateEvents = events
					.Where(userEvent => userEvent.CompleteDate > startOfDay
									  && userEvent.StartDate < endOfDay)
					.ToList();

				foreach (var dateEvent in dateEvents)
				{
					var estimatedStartDate = dateEvent.StartDate > startOfDay
						? dateEvent.StartDate
						: startOfDay;
					var estimatedEndDate = dateEvent.CompleteDate < endOfDay
						? dateEvent.CompleteDate
						: endOfDay;

					// Every field of the event is named here, so a field left out is a
					// field a multi-day event silently loses. Keep the list exhaustive.
					yield return new UserEvent
					{
						Issue = dateEvent.Issue,
						StartDate = estimatedStartDate,
						CompleteDate = estimatedEndDate,
						Author = dateEvent.Author,
						Source = dateEvent.Source,
						Summary = dateEvent.Summary,
						Details = dateEvent.Details
					};
				}
				day = day.AddDays(-1);
			}
		}
	}
}
