using CotadorLogistico.Core.ExchangeRates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CotadorLogistico.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/exchange-rates")]
public sealed class ExchangeRatesController : ControllerBase
{
    private readonly IExchangeRateRepository _repository;

    public ExchangeRatesController(IExchangeRateRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExchangeRateSnapshot>>> GetAsync(CancellationToken cancellationToken)
    {
        var rates = await _repository.GetAllAsync(cancellationToken);
        return Ok(rates);
    }
}
