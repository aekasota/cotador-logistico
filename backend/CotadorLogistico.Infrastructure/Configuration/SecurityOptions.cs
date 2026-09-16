namespace CotadorLogistico.Infrastructure.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public IReadOnlyList<string> AllowedEmailDomains { get; set; } = [];
}
