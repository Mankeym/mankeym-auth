namespace AuthService.Api.Common.Authorization;

public static class Permissions
{
    public const string AuditView = "audit:view";

    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";

    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";

    public const string AuditRead = "audit.read";

    public static IEnumerable<string> GetAll()
    {
        yield return UsersRead;
        yield return UsersManage;
        yield return RolesRead;
        yield return RolesManage;
        yield return AuditRead;
    }
}
