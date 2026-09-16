using CotadorLogistico.Core.Domain;

namespace CotadorLogistico.Tests.TestDoubles;

internal sealed class FakeSupabaseAuthAdminClient : ISupabaseAuthAdminClient
{
    public List<(string Email, string Password)> Calls { get; } = new();
    public List<(Guid UserId, string? NewEmail, string? NewPassword)> UpdateCalls { get; } = new();
    public Guid NextUserId { get; set; } = Guid.NewGuid();

    public Task<Guid> CreateUserAsync(string email, string password, CancellationToken cancellationToken)
    {
        Calls.Add((email, password));
        return Task.FromResult(NextUserId);
    }

    public Task UpdateUserAsync(Guid userId, string? newEmail, string? newPassword, CancellationToken cancellationToken)
    {
        UpdateCalls.Add((userId, newEmail, newPassword));
        return Task.CompletedTask;
    }
}
