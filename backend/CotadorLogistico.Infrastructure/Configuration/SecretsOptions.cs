namespace CotadorLogistico.Infrastructure.Configuration;

public enum SecretsStoreMode
{
    Vault,

    Aes
}

public sealed class SecretsOptions
{
    public const string SectionName = "Secrets";

    public SecretsStoreMode Mode { get; set; } = SecretsStoreMode.Vault;

    public string? MasterKeyBase64 { get; set; }
}
