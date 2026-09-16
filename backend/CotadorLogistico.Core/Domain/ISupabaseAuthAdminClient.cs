namespace CotadorLogistico.Core.Domain;

public interface ISupabaseAuthAdminClient
{
    Task<Guid> CreateUserAsync(string email, string password, CancellationToken cancellationToken);

    Task UpdateUserAsync(Guid userId, string? newEmail, string? newPassword, CancellationToken cancellationToken);
}

public sealed class SupabaseAdminException(string message) : Exception(message);
