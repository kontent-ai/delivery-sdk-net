namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Base class for response objects.
    /// </summary>
    public abstract class AbstractResponse
    {
        #region "Debugging properties"

        /// <summary>
        /// The URL of the request sent to the Kentico Cloud endpoint by the <see cref="DeliveryClient"/>.
        /// Useful for debugging.
        /// </summary>
        public string ApiUrl { get; protected set; }

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="apiUrl">API URL used to communicate with the underlying Kentico Cloud endpoint.</param>
        protected AbstractResponse(string apiUrl)
        {
            ApiUrl = apiUrl;
        }
    }
}
