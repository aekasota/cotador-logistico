using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Infrastructure.Shipping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CotadorLogistico.Api.Controllers;

[Route("api/shipping")]
[EnableRateLimiting(RateLimitPolicies.Shipping)]
public sealed class ShippingController : CotadorControllerBase
{
    private readonly FrenetApiProxy _frenet;
    private readonly MelhorEnvioApiProxy _melhorEnvio;
    private readonly ILogger<ShippingController> _logger;

    public ShippingController(
        ICurrentUserAccessor currentUser, FrenetApiProxy frenet, MelhorEnvioApiProxy melhorEnvio,
        ILogger<ShippingController> logger)
        : base(currentUser)
    {
        _frenet = frenet;
        _melhorEnvio = melhorEnvio;
        _logger = logger;
    }

    [HttpPost("frenet")]
    public Task<IActionResult> PostFrenetAsync(CancellationToken cancellationToken) =>
        ForwardAsync(_frenet, cancellationToken);

    [HttpPost("melhorenvio")]
    public Task<IActionResult> PostMelhorEnvioAsync(CancellationToken cancellationToken) =>
        ForwardAsync(_melhorEnvio, cancellationToken);

    private async Task<IActionResult> ForwardAsync(Core.Shipping.IShippingApiProxy proxy, CancellationToken cancellationToken)
    {
        var userId = CurrentProfile.Id;

        if (!await proxy.IsConfiguredAsync(userId, cancellationToken))
        {
            _logger.LogWarning("Cotação recusada: {Carrier} ainda não está configurada.", proxy.CarrierName);
            return BadRequest(new { error = $"A API da {proxy.CarrierName} ainda não foi configurada." });
        }

        string requestBody;
        using (var reader = new StreamReader(Request.Body))
            requestBody = await reader.ReadToEndAsync(cancellationToken);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await proxy.ForwardAsync(userId, requestBody, cancellationToken);
            stopwatch.Stop();
            _logger.LogInformation(
                "Cotação {Carrier}: HTTP {StatusCode} em {ElapsedMs} ms (usuário {UserId}).",
                proxy.CarrierName, result.StatusCode, stopwatch.ElapsedMilliseconds, CurrentProfile.Id);

            return new ContentResult
            {
                Content = result.Body,
                ContentType = result.ContentType,
                StatusCode = result.StatusCode
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Falha ao contatar {Carrier} após {ElapsedMs} ms.", proxy.CarrierName, stopwatch.ElapsedMilliseconds);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = $"Falha ao contatar {proxy.CarrierName}. Tente novamente." });
        }
    }
}
