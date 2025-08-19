using System;
using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions.SharedModels;

/// <summary>
/// Represents the result of a Delivery API operation, providing functional error handling.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public interface IDeliveryResult<out T>
{
    /// <summary>
    /// Gets the result value when the operation was successful.
    /// </summary>
    T Value { get; }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the requested content was not found.
    /// </summary>
    bool IsNotFound { get; }

    /// <summary>
    /// Gets a value indicating whether the operation was rate limited.
    /// </summary>
    bool IsRateLimited { get; }

    /// <summary>
    /// Gets the collection of errors that occurred during the operation.
    /// </summary>
    IReadOnlyList<IDeliveryError> Errors { get; }

    /// <summary>
    /// Gets rate limiting information, if available.
    /// </summary>
    IRateLimitInfo? RateLimit { get; }

    /// <summary>
    /// Gets the HTTP status code of the response.
    /// </summary>
    int StatusCode { get; }

    /// <summary>
    /// Gets a value indicating whether the content is stale.
    /// Stale content indicates that there is a more recent version, but it will become available later.
    /// </summary>
    bool HasStaleContent { get; }

    /// <summary>
    /// Gets the continuation token for pagination, if applicable.
    /// </summary>
    string? ContinuationToken { get; }

    /// <summary>
    /// Gets the URL used to retrieve this response for debugging purposes.
    /// </summary>
    string? RequestUrl { get; }
}

/// <summary>
/// Represents information about rate limiting.
/// </summary>
public interface IRateLimitInfo
{
    /// <summary>
    /// Gets the number of requests remaining in the current window.
    /// </summary>
    int? Remaining { get; }

    /// <summary>
    /// Gets the total number of requests allowed in the current window.
    /// </summary>
    int? Limit { get; }

    /// <summary>
    /// Gets the time when the rate limit window resets.
    /// </summary>
    DateTimeOffset? Reset { get; }

    /// <summary>
    /// Gets the retry-after duration if rate limited.
    /// </summary>
    TimeSpan? RetryAfter { get; }
}

/// <summary>
/// Represents an error from the Delivery API.
/// </summary>
public interface IDeliveryError
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the error code from the Delivery API.
    /// </summary>
    string? ErrorCode { get; }

    /// <summary>
    /// Gets the request ID for troubleshooting.
    /// </summary>
    string? RequestId { get; }

    /// <summary>
    /// Gets additional error details.
    /// </summary>
    IReadOnlyDictionary<string, object>? Details { get; }
}