namespace CotadorLogistico.Core.Domain;

public sealed class Quote
{
    public Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public required string SourceCep { get; init; }
    public required string DestinationCep { get; init; }
    public string? DestinationLabel { get; init; }
    public required decimal PackageWeightKg { get; init; }
    public required decimal PackageLengthCm { get; init; }
    public required decimal PackageWidthCm { get; init; }
    public required decimal PackageHeightCm { get; init; }
    public int PackageQuantity { get; init; } = 1;
    public required decimal DeclaredValueBrl { get; init; }
    public required bool ComparisonMode { get; init; }
    public required string Currency { get; init; }
    public decimal? ExchangeRateUsed { get; init; }
    public bool IsDemo { get; init; }
}

public sealed class QuoteOption
{
    public Guid Id { get; init; }
    public required Guid QuoteId { get; init; }

    public required string Provider { get; init; }
    public required string Carrier { get; init; }
    public required string ServiceName { get; init; }
    public string? ServiceCode { get; init; }
    public required decimal PriceBrl { get; init; }
    public required int DeliveryDays { get; init; }
    public bool IsWinnerPrice { get; init; }
    public bool IsWinnerTime { get; init; }
    public bool WasSelectedInComparison { get; init; }
    public string? RawMetadataJson { get; init; }
}

public static class ShippingProvider
{
    public const string Frenet = "FRENET";
    public const string MelhorEnvio = "MELHOR_ENVIO";
}
