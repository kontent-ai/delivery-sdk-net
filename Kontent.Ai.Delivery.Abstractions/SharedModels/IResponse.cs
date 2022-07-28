namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// A base interface for all response envelopes.
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// A successful JSON response from the Kontent.ai Delivery API.
        /// </summary>
        public IApiResponse ApiResponse { get; }
    }
}
