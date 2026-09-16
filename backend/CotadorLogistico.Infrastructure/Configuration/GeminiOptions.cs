namespace CotadorLogistico.Infrastructure.Configuration;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string Model { get; set; } = "gemini-3.6-flash";

    public string ApiBaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    public int TimeoutSeconds { get; set; } = 20;
}
