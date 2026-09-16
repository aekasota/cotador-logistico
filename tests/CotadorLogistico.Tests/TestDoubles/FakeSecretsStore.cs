using CotadorLogistico.Core.Secrets;

namespace CotadorLogistico.Tests.TestDoubles;

internal sealed class FakeSecretsStore : ISecretsStore
{
    private readonly Dictionary<(Guid UserId, string Key), string> _values = new();

    public FakeSecretsStore(Dictionary<string, string>? initial = null, Guid? userId = null)
    {
        var scopedUserId = userId ?? FakeCurrentUserAccessor.DefaultUserId;
        if (initial is null) return;
        foreach (var (key, value) in initial) _values[(scopedUserId, key)] = value;
    }

    public Task<string?> GetAsync(Guid userId, string key, CancellationToken cancellationToken) =>
        Task.FromResult(_values.GetValueOrDefault((userId, key)));

    public Task SetAsync(Guid userId, string key, string value, CancellationToken cancellationToken)
    {
        _values[(userId, key)] = value;
        return Task.CompletedTask;
    }

    public Task<bool> IsConfiguredAsync(Guid userId, string key, CancellationToken cancellationToken) =>
        Task.FromResult(_values.ContainsKey((userId, key)));

    public Task RemoveAsync(Guid userId, string key, CancellationToken cancellationToken)
    {
        _values.Remove((userId, key));
        return Task.CompletedTask;
    }
}
