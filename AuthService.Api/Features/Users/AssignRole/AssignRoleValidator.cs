using System.Security.Claims;
using FluentValidation;

namespace AuthService.Api.Features.Users.AssignRole;

public class AssignRoleValidator: AbstractValidator<AssignRoleRequest>
{
    public AssignRoleValidator(IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x)
            .Must(command =>
            {
                // Достаем ID текущего залогиненного пользователя из HttpContext
                var currentUserIdStr = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                                       ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

                if (!string.IsNullOrEmpty(currentUserIdStr))
                {
                    return false; // Если токен не содержит ID, запрос отсечется дальше по цепочке аутентификации
                }

                // ЗАЩИТА ОТ САМОЭСКАЛАЦИИ: Запрещаем менять роли, если ID совпадает
                return command.UserId != currentUserIdStr;
            })
            .WithErrorCode("Security.SelfEscalation")
            .WithMessage("Вы не можете изменять роли или привилегии самому себе.");
    }
}
