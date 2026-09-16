using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Core.Ai;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CotadorLogistico.Api.Controllers;

[Route("api/ai")]
[EnableRateLimiting(RateLimitPolicies.Ai)]
public sealed class AiController : CotadorControllerBase
{
    private readonly IPackageDimensionEstimator _estimator;

    public AiController(ICurrentUserAccessor currentUser, IPackageDimensionEstimator estimator) : base(currentUser)
    {
        _estimator = estimator;
    }

    [HttpPost("package-dimensions")]
    public async Task<ActionResult<PackageDimensionsResponse>> EstimateAsync(
        [FromBody] PackageDimensionsRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentProfile.Id;

        var result = await _estimator.EstimateAsync(userId, request.ProductDescription, cancellationToken);
        return Ok(result);
    }
}
