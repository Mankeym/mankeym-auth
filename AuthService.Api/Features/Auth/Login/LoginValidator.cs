using FluentValidation;

namespace AuthService.Api.Features.Auth.Login;

public class LoginValidator :  AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        // 2. Проверка пароля
        RuleFor(x => x.Password) // Замените на x.password
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
    }
}
