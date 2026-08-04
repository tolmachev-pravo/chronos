using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Chronos.Application.Worklogs.Dto
{
    /// <summary>
    /// Settings of working day
    /// </summary>
    public class WorkingDaySettings : IValidatableObject
    {
        /// <summary>
        /// Working start time
        /// </summary>
        [Required]
        public TimeSpan WorkingStartTime { get; set; }

        /// <summary>
        /// Working end time
        /// </summary>
        [Required]
        public TimeSpan WorkingEndTime { get; set; }

        /// <summary>
        /// Time for lunch
        /// </summary>
        [Required]
        public TimeSpan LunchTime { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Working time
        /// </summary>
        public TimeSpan WorkingTime => WorkingEndTime - WorkingStartTime - LunchTime;

        public WorkingDaySettings(
            TimeSpan workingStartTime,
            TimeSpan workingEndTime,
            TimeSpan lunchTime)
        {
            WorkingStartTime = workingStartTime;
            WorkingEndTime = workingEndTime;
            LunchTime = lunchTime;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (WorkingStartTime > WorkingEndTime)
            {
                yield return new ValidationResult(
                    errorMessage: "End time must be greater than start time",
                    memberNames: new List<string> { nameof(WorkingStartTime), nameof(WorkingEndTime) });
            }
        }
    }
}
