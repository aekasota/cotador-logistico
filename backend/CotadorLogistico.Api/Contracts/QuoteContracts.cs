namespace CotadorLogistico.Api.Contracts;

public sealed record PersistQuoteRequest(
    string SourceCep,
    string DestinationCep,
    string? DestinationLabel,
    decimal PackageWeightKg,
    decimal PackageLengthCm,
    decimal PackageWidthCm,
    decimal PackageHeightCm,
    int PackageQuantity,
    decimal DeclaredValueBrl,
    bool ComparisonMode,
    string Currency,
    decimal? ExchangeRateUsed,
    IReadOnlyList<PersistQuoteOptionRequest> Options);

public sealed record PersistQuoteOptionRequest(
    string Provider,
    string Carrier,
    string ServiceName,
    string? ServiceCode,
    decimal PriceBrl,
    int DeliveryDays,
    bool IsWinnerPrice,
    bool IsWinnerTime,
    bool WasSelectedInComparison);

public sealed record PersistQuoteResponse(Guid Id);
