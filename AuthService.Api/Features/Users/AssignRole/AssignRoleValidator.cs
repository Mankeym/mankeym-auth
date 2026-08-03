using System.Security.Claims;
using FluentValidation;

namespace AuthService.Api.Features.Users.AssignRole;

public class AssignRoleValidator : AbstractValidator<AssignRoleRequest>
{
    public AssignRoleValidator(IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x)
            .Must(command =>
            {
                var currentUserIdStr = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                                       ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

                if (!string.IsNullOrEmpty(currentUserIdStr))
                {
                    return false;
                }

                return command.UserId != currentUserIdStr;
            })
            .WithErrorCode("Security.SelfEscalation")
            .WithMessage("Вы не можете изменять роли или привилегии самому себе.");
    }
}
