namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Keeps settings which are provided by customer or have default values, used in <see cref="DeliveryClient"/>.
    /// </summary>
    public class DeliveryOptions
    {
        /// <summary>
        /// Gets or sets the Production endpoint address.
        /// </summary>
        public string ProductionEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the Preview endpoint address.
        /// </summary>
        public string PreviewEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the Project identifier.
        /// </summary>
        public string ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the Preview API key.
        /// </summary>
        public string PreviewApiKey { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryOptions"/> class.
        /// </summary>
        public DeliveryOptions()
        {
            ProductionEndpoint = "https://deliver.kenticocloud.com/{0}";
            PreviewEndpoint = "https://preview-deliver.kenticocloud.com/{0}";
        }
    }
}
