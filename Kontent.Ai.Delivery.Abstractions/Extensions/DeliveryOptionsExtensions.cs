namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// A class which contains extension methods on <see cref="DeliveryOptions"/>.
    /// </summary>
    public static class DeliveryOptionsExtensions
    {
        /// <summary>
        /// Maps one <see cref="DeliveryOptions"/> object to another.
        /// </summary>
        /// <param name="o">A destination.</param>
        /// <param name="options">A source.</param>
        public static void Configure(this DeliveryOptions o, DeliveryOptions options)
        {
            o.EnvironmentId = options.EnvironmentId;
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
            // See #312
            #pragma warning disable CS0618
            o.Name = options.Name;
            #pragma warning restore CS0618
            o.DefaultRenditionPreset = options.DefaultRenditionPreset;
        }
    }
}
