using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Api.Contracts;
using CotadorLogistico.Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace CotadorLogistico.Api.Controllers;

[Route("api/quotes")]
public sealed class QuotesController : CotadorControllerBase
{
    private const int MaxDeclaredValueBrl = 1_000_000;
    private const decimal MaxWeightKg = 1_000;
    private const decimal MaxDimensionCm = 1_000;

    private readonly IQuoteRepository _quotes;
    private readonly ILogger<QuotesController> _logger;

    public QuotesController(ICurrentUserAccessor currentUser, IQuoteRepository quotes, ILogger<QuotesController> logger)
        : base(currentUser)
    {
        _quotes = quotes;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<PersistQuoteResponse>> PersistAsync(
        [FromBody] PersistQuoteRequest request, CancellationToken cancellationToken)
    {
        var profile = CurrentProfile;

        var validationError = Validate(request);
        if (validationError is not null) return BadRequest(new { error = validationError });

        var quote = new Quote
        {
            UserId = profile.Id,
            SourceCep = request.SourceCep,
            DestinationCep = request.DestinationCep,
            DestinationLabel = request.DestinationLabel,
            PackageWeightKg = request.PackageWeightKg,
            PackageLengthCm = request.PackageLengthCm,
            PackageWidthCm = request.PackageWidthCm,
            PackageHeightCm = request.PackageHeightCm,
            PackageQuantity = request.PackageQuantity,
            DeclaredValueBrl = request.DeclaredValueBrl,
            ComparisonMode = request.ComparisonMode,
            Currency = request.Currency,
            ExchangeRateUsed = request.ExchangeRateUsed,
            IsDemo = false
        };

        var options = request.Options.Select(o => new QuoteOption
        {
            QuoteId = Guid.Empty,
            Provider = o.Provider,
            Carrier = o.Carrier,
            ServiceName = o.ServiceName,
            ServiceCode = o.ServiceCode,
            PriceBrl = o.PriceBrl,
            DeliveryDays = o.DeliveryDays,
            IsWinnerPrice = o.IsWinnerPrice,
            IsWinnerTime = o.IsWinnerTime,
            WasSelectedInComparison = o.WasSelectedInComparison
        }).ToList();

        var id = await _quotes.InsertAsync(quote, options, cancellationToken);
        _logger.LogInformation(
            "Cotação registrada (usuário={UserId}, destino={Destination}, opções={OptionCount}).",
            profile.Id, request.DestinationLabel ?? request.DestinationCep, options.Count);

        return Ok(new PersistQuoteResponse(id));
    }

    [HttpGet("metrics/price-advantage")]
    public async Task<ActionResult<IReadOnlyList<CapitalAdvantage>>> GetPriceAdvantageAsync(CancellationToken cancellationToken)
    {
        var profile = CurrentProfile;

        IReadOnlyCollection<Guid>? userIds = profile.Role switch
        {
            Role.Owner => null,
            Role.Supervisor => null,
            _ => new[] { profile.Id }
        };

        if (profile.Role == Role.Supervisor)
        {
            var teamCounts = await _quotes.CountByUserForTeamAsync(profile.Id, cancellationToken);
            userIds = teamCounts.Keys.Append(profile.Id).ToArray();
        }

        var advantages = await _quotes.GetPriceAdvantageByDestinationAsync(profile.OrganizationId, userIds, cancellationToken);
        return Ok(advantages);
    }

    private static string? Validate(PersistQuoteRequest request)
    {
        if (request.SourceCep is not { Length: 8 } || !request.SourceCep.All(char.IsDigit))
            return "CEP de origem inválido.";
        if (request.DestinationCep is not { Length: 8 } || !request.DestinationCep.All(char.IsDigit))
            return "CEP de destino inválido.";
        if (request.PackageWeightKg <= 0 || request.PackageWeightKg > MaxWeightKg)
            return "Peso inválido.";
        if (request.PackageLengthCm <= 0 || request.PackageLengthCm > MaxDimensionCm ||
            request.PackageWidthCm <= 0 || request.PackageWidthCm > MaxDimensionCm ||
            request.PackageHeightCm <= 0 || request.PackageHeightCm > MaxDimensionCm)
            return "Dimensões inválidas.";
        if (request.DeclaredValueBrl < 0 || request.DeclaredValueBrl > MaxDeclaredValueBrl)
            return "Valor declarado inválido.";
        if (request.Currency is not ("BRL" or "USD" or "MXN"))
            return "Moeda inválida.";
        if (request.Options.Count == 0)
            return "Nenhuma opção de frete informada.";
        if (request.Options.Any(o => o.Provider is not ("FRENET" or "MELHOR_ENVIO")))
            return "Provider de frete inválido.";

        return null;
    }
}
