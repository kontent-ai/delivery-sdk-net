namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    public interface IResponse
    {
        /// <summary>
        /// The successful JSON response from Kentico Kontent Delivery API.
        /// </summary>
        public IApiResponse ApiResponse { get; }
    }
}
