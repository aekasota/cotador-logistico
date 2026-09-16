namespace CotadorLogistico.Core.Secrets;

public interface ISecretsStore
{
    Task<string?> GetAsync(Guid userId, string key, CancellationToken cancellationToken);

    Task SetAsync(Guid userId, string key, string value, CancellationToken cancellationToken);

    Task<bool> IsConfiguredAsync(Guid userId, string key, CancellationToken cancellationToken);

    Task RemoveAsync(Guid userId, string key, CancellationToken cancellationToken);
}

public static class SecretKeys
{
    public const string FrenetToken = "frenet_token";
    public const string MelhorEnvioToken = "melhor_envio_token";
    public const string GeminiApiKey = "gemini_api_key";
}
