using FluentValidation;

namespace AuthService.Api.Features.Auth.Register;

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[@#$%^&+=!]").WithMessage("Password must contain at least one special character (@, #, $, %, ^, &, +, =, !).")
            .Must(NotBeACommonPassword).WithMessage("This password is too common and insecure.");
    }

    private bool NotBeACommonPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;

        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Password123!", "Qwerty123!", "12345678!", "Admin123!"
        };

        return !commonPasswords.Contains(password);
    }
}
