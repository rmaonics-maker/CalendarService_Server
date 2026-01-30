using Calendar_Service.Contracts;
using FluentValidation;

namespace Calendar_Service.Validators
{
    public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateUserRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Имя пользователя обязательно.")
                .MaximumLength(50).WithMessage("Имя не может быть длиннее 50 знаков.");
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Почта обязательна")
                .EmailAddress().WithMessage("Требуется действующий адрес почты");
        }
    }
}
