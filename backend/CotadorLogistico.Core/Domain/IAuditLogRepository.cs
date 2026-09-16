namespace CotadorLogistico.Core.Domain;

public interface IAuditLogRepository
{
    Task RecordAsync(
        Guid organizationId, Guid? actorId, string action, Guid? targetUserId,
        IReadOnlyDictionary<string, string>? metadata, CancellationToken cancellationToken);
}

public static class AuditActions
{
    public const string UserCreated = "user.created";
    public const string UserUpdated = "user.updated";
    public const string UserActivated = "user.activated";
    public const string UserDeactivated = "user.deactivated";
    public const string TemporaryPasswordIssued = "user.temporary_password_issued";
    public const string PasswordChangedBySelf = "user.password_changed_by_self";
    public const string IntegrationReset = "integration.reset";
}
