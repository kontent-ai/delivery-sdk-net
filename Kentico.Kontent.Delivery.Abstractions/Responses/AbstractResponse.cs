namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    /// <summary>
    /// Represents a successful response from Kentico Kontent Delivery API.
    /// </summary>
    public abstract class AbstractResponse : IResponse
    {
        /// <summary>
        /// The successful JSON response from Kentico Kontent Delivery API.
        /// </summary>
        public IApiResponse ApiResponse { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractResponse"/> class.
        /// </summary>
        /// <param name="response">A successful JSON response from Kentico Kontent Delivery API.</param>
        protected AbstractResponse(IApiResponse response)
        {
            ApiResponse = response;
        }
    }
}
