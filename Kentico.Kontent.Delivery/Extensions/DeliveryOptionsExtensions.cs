using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.Extensions
{
    internal static class DeliveryOptionsExtensions
    {
        public static void Configure(this DeliveryOptions o, DeliveryOptions options)
        {
            o.ProjectId = options.ProjectId;
            o.ProductionEndpoint = options.ProductionEndpoint;
            o.PreviewEndpoint = options.PreviewEndpoint;
            o.PreviewApiKey = options.PreviewApiKey;
            o.UsePreviewApi = options.UsePreviewApi;
            o.WaitForLoadingNewContent = options.WaitForLoadingNewContent;
            o.UseSecureAccess = options.UseSecureAccess;
            o.SecureAccessApiKey = options.SecureAccessApiKey;
            o.EnableRetryPolicy = options.EnableRetryPolicy;
            o.DefaultRetryPolicyOptions = options.DefaultRetryPolicyOptions;
            o.IncludeTotalCount = options.IncludeTotalCount;
        }
    }
}
