namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// A class which contains extension methods on <see cref="DeliveryOptions"/>.
/// </summary>
public static class DeliveryOptionsExtensions
{

    /// <summary>
    /// Gets the base URL for the delivery API.
    /// </summary>
    /// <param name="options">The delivery options.</param>
    /// <returns>The base URL for the delivery API.</returns>
    public static string GetBaseUrl(this DeliveryOptions options) => options.UsePreviewApi ? options.PreviewEndpoint : options.ProductionEndpoint;

    /// <summary>
    /// Gets the API key for the delivery API.
    /// </summary>
    /// <param name="options">The delivery options.</param>
    /// <returns>The API key for the delivery API.</returns>
    public static string? GetApiKey(this DeliveryOptions options) => options.UseSecureAccess ? options.SecureAccessApiKey : options.UsePreviewApi ? options.PreviewApiKey : null;
}
