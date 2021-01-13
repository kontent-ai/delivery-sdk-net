using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.Extensions
{
    /// <summary>
    /// A class which contains extension methods on <see cref="DeliveryOptions"/>.
    /// </summary>
    public static class DeliveryOptionsExtensions
    {
        /// <summary>
        /// Maps a <see cref="DeliveryOptions"/> to each other.
        /// </summary>
        /// <param name="o">A destination.</param>
        /// <param name="options">A source.</param>
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
