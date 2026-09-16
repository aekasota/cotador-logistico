namespace CotadorLogistico.Core.Shipping;

public interface IShippingApiProxy
{
    string CarrierName { get; }

    string SettingsKey { get; }

    Task<bool> IsConfiguredAsync(Guid userId, CancellationToken cancellationToken);

    Task<ProxyResult> ForwardAsync(Guid userId, string requestBody, CancellationToken cancellationToken);
}

public sealed record ProxyResult(int StatusCode, string Body, string ContentType = "application/json");
