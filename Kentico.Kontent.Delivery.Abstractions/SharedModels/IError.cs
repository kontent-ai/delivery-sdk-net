namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a faulty JSON response from Kentico Kontent Delivery API.
    /// </summary>
    public interface IError
    {
        /// <summary>
        /// Gets error Message.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the ID of a request that can be used for troubleshooting.
        /// </summary>
        string RequestId { get; }

        /// <summary>
        /// Gets Kentico Kontent Delivery API error code. Check the Message property for more information
        /// </summary>
        int ErrorCode { get; }

        /// <summary>
        /// Gets specific code of error.
        /// </summary>
        int SpecificCode { get; }
    }
}
