namespace CotadorLogistico.Core.Ai;

public interface IPackageDimensionEstimator
{
    Task<PackageDimensionsResponse> EstimateAsync(Guid userId, string productDescription, CancellationToken cancellationToken);
}
