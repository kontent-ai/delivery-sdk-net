namespace Kontent.Ai.Delivery.Extensions;

/// <summary>
/// Extensions for the <see cref="DeliveryOptions"/> class.
/// </summary>
internal static class DeliveryOptionsExtensions
{
    /// <summary>
    /// Maps one delivery options to another.
    /// </summary>
    /// <param name="o">A destination.</param>
    /// <param name="options">A source.</param>
    public static void Configure(this DeliveryOptions o, DeliveryOptions options)
    {
        o.EnvironmentId = options.EnvironmentId;
        o.PreviewApiKey = options.PreviewApiKey;
        o.SecureAccessApiKey = options.SecureAccessApiKey;
        o.UsePreviewApi = options.UsePreviewApi;
        o.UseSecureAccess = options.UseSecureAccess;
        o.EnableResilience = options.EnableResilience;
        o.ProductionEndpoint = options.ProductionEndpoint;
        o.PreviewEndpoint = options.PreviewEndpoint;
        o.IncludeTotalCount = options.IncludeTotalCount;
        o.WaitForLoadingNewContent = options.WaitForLoadingNewContent;
        o.RenderRichTextToHtml = options.RenderRichTextToHtml;
        o.DefaultRenditionPreset = options.DefaultRenditionPreset;
    }
}
