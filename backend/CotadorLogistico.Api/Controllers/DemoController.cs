using CotadorLogistico.Core.Demo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CotadorLogistico.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/demo")]
[EnableRateLimiting(RateLimitPolicies.Demo)]
public sealed class DemoController : ControllerBase
{
    private readonly IFakeQuoteGenerator _fakeQuoteGenerator;
    private readonly ILogger<DemoController> _logger;

    public DemoController(IFakeQuoteGenerator fakeQuoteGenerator, ILogger<DemoController> logger)
    {
        _fakeQuoteGenerator = fakeQuoteGenerator;
        _logger = logger;
    }

    [HttpPost("frenet")]
    public Task<IActionResult> PostFrenetAsync(CancellationToken cancellationToken) => GenerateAsync("Frenet", cancellationToken);

    [HttpPost("melhorenvio")]
    public Task<IActionResult> PostMelhorEnvioAsync(CancellationToken cancellationToken) => GenerateAsync("Melhor Envio", cancellationToken);

    private async Task<IActionResult> GenerateAsync(string carrierName, CancellationToken cancellationToken)
    {
        string requestBody;
        using (var reader = new StreamReader(Request.Body))
            requestBody = await reader.ReadToEndAsync(cancellationToken);

        if (requestBody.Length > 4096)
            return BadRequest(new { error = "Corpo da requisição muito grande." });

        var result = _fakeQuoteGenerator.Generate(carrierName, requestBody);
        _logger.LogInformation("Cotação demo gerada para {Carrier}.", carrierName);

        return new ContentResult { Content = result.Body, ContentType = result.ContentType, StatusCode = result.StatusCode };
    }
}
