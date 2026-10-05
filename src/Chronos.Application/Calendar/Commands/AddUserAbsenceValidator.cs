using FluentValidation;

namespace Chronos.Application.Calendar.Commands
{
    public class AddUserAbsenceValidator : AbstractValidator<AddUserAbsence.Command>
    {
        public AddUserAbsenceValidator()
        {
            RuleFor(command => command.Username)
                .NotEmpty();

            RuleFor(command => command.EndDate.Date)
                .GreaterThanOrEqualTo(command => command.StartDate.Date)
                .WithMessage("Последний день отсутствия не может быть раньше первого");

            RuleFor(command => command.Kind)
                .IsInEnum();

            RuleFor(command => command.Comment)
                .MaximumLength(200)
                .WithMessage("Комментарий должен быть не длиннее 200 символов");
        }
    }
}
