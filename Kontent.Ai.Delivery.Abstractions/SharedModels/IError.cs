namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a faulty JSON response from Kontent.ai Delivery API.
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
    string? RequestId { get; }

    /// <summary>
    /// Gets Kontent.ai Delivery API error code. Check the Message property for more information
    /// </summary>
    int? ErrorCode { get; }

    /// <summary>
    /// Gets specific code of error.
    /// </summary>
    int? SpecificCode { get; }

    /// <summary>
    /// Gets the underlying exception that caused the error, if available.
    /// Useful for debugging and accessing detailed error information such as stack traces.
    /// </summary>
    /// <remarks>
    /// This may contain the HTTP client exception (e.g., from Refit) which includes
    /// additional context like the original request and response details.
    /// </remarks>
    Exception? Exception { get; }
}
