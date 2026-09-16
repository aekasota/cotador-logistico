namespace CotadorLogistico.Infrastructure.Configuration;

public sealed class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public required string Url { get; set; }

    public required string SecretKey { get; set; }

    public required string DbConnectionString { get; set; }

    public string? LegacyJwtSecret { get; set; }

    public string AuthBaseUrl => $"{Url.TrimEnd('/')}/auth/v1";
}
