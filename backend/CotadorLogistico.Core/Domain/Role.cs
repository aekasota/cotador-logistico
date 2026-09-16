namespace CotadorLogistico.Core.Domain;

public enum Role
{
    Operator = 0,
    Supervisor = 1,
    Owner = 2
}

public static class RoleExtensions
{
    public static bool IsAtLeast(this Role role, Role minimum) => role >= minimum;

    public static bool TryParse(string? value, out Role role)
    {
        role = Role.Operator;
        if (string.IsNullOrWhiteSpace(value)) return false;
        return Enum.TryParse(value, ignoreCase: true, out role);
    }

    public static string ToDbString(this Role role) => role switch
    {
        Role.Owner => "OWNER",
        Role.Supervisor => "SUPERVISOR",
        _ => "OPERATOR"
    };
}
