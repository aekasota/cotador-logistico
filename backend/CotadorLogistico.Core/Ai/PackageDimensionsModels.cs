using System.Text.Json.Serialization;

namespace CotadorLogistico.Core.Ai;

public sealed record PackageDimensionsRequest(
    [property: JsonPropertyName("productDescription")] string ProductDescription);

public sealed record PackageDimensionsResponse(
    [property: JsonPropertyName("allowed")] bool Allowed,
    [property: JsonPropertyName("suggestions")] IReadOnlyList<PackageDimensionSuggestion> Suggestions,
    [property: JsonPropertyName("warning")] string Warning)
{
    public static PackageDimensionsResponse Refused(string warning) =>
        new(Allowed: false, Suggestions: Array.Empty<PackageDimensionSuggestion>(), Warning: warning);
}

public sealed record PackageDimensionSuggestion(
    [property: JsonPropertyName("lengthCm")] double LengthCm,
    [property: JsonPropertyName("widthCm")] double WidthCm,
    [property: JsonPropertyName("heightCm")] double HeightCm,
    [property: JsonPropertyName("confidence")] string Confidence,
    [property: JsonPropertyName("reason")] string Reason);
