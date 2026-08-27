using FluentValidation;
using System;

namespace Chronos.Application.Users.Commands
{
    /// <summary>
    /// Keeps the working day meaningful: a day that ends before it starts, or one entirely
    /// eaten by lunch, would leave no room to place estimated worklogs.
    /// </summary>
    public class UpsertUserSettingsValidator : AbstractValidator<UpsertUserSettings.Command>
    {
        public UpsertUserSettingsValidator()
        {
            RuleFor(command => command.Username)
                .NotEmpty();

            RuleFor(command => command.Settings.WorkingEndTime)
                .GreaterThan(command => command.Settings.WorkingStartTime)
                .WithMessage("Время окончания работы должно быть больше времени начала");

            RuleFor(command => command.Settings.LunchTime)
                .GreaterThanOrEqualTo(TimeSpan.Zero)
                .WithMessage("Обед не может быть отрицательным");

            RuleFor(command => command.Settings.LunchTime)
                .LessThan(command => command.Settings.WorkingEndTime - command.Settings.WorkingStartTime)
                .WithMessage("Обед должен быть короче рабочего дня");
        }
    }
}
