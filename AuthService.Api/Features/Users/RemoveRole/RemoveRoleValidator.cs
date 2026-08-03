using System.Security.Claims;
using FluentValidation;

namespace AuthService.Api.Features.Users.RemoveRole;

public class RemoveRoleValidator : AbstractValidator<RemoveRoleRequest>
{
    public RemoveRoleValidator(IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.");

        RuleFor(x => x)
            .Must(request =>
            {
                var currentUserIdStr = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                                       ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

                if (string.IsNullOrEmpty(currentUserIdStr))
                {
                    return true;
                }

                return request.UserId != currentUserIdStr;
            })
            .WithErrorCode("Security.SelfEscalation")
            .WithMessage("You cannot remove a role from yourself.");
    }
}
