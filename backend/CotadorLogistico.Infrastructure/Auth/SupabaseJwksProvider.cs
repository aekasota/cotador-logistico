using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CotadorLogistico.Infrastructure.Auth;

public sealed class SupabaseJwksProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _jwksUrl;
    private readonly ILogger<SupabaseJwksProvider> _logger;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private JsonWebKeySet? _cachedKeySet;
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;

    public SupabaseJwksProvider(IHttpClientFactory httpClientFactory, string supabaseUrl, ILogger<SupabaseJwksProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _jwksUrl = $"{supabaseUrl.TrimEnd('/')}/auth/v1/.well-known/jwks.json";
        _logger = logger;
    }

    public IEnumerable<SecurityKey> ResolveSigningKeys(string? kid)
    {
        var keySet = GetOrRefreshAsync().GetAwaiter().GetResult();

        if (kid is not null && keySet.Keys.All(k => k.Kid != kid))
        {
            keySet = ForceRefreshAsync().GetAwaiter().GetResult();
        }

        return keySet.GetSigningKeys();
    }

    private async Task<JsonWebKeySet> GetOrRefreshAsync()
    {
        if (_cachedKeySet is not null && DateTimeOffset.UtcNow - _cachedAt < CacheDuration)
            return _cachedKeySet;

        return await ForceRefreshAsync();
    }

    private async Task<JsonWebKeySet> ForceRefreshAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(SupabaseJwksProvider));
            var json = await client.GetStringAsync(_jwksUrl);
            var keySet = new JsonWebKeySet(json);

            _cachedKeySet = keySet;
            _cachedAt = DateTimeOffset.UtcNow;
            return keySet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao buscar o JWKS do Supabase em {JwksUrl}.", _jwksUrl);

            if (_cachedKeySet is not null) return _cachedKeySet;
            throw;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
