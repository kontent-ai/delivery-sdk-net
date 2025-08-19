using System.Net;
using Kontent.Ai.Delivery.Abstractions.SharedModels;

namespace Kontent.Ai.Delivery.SharedModels;

/// <summary>
/// Concrete implementation of <see cref="IDeliveryResult{T}"/> for functional error handling.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
internal sealed class DeliveryResult<T> : IDeliveryResult<T>
{
    /// <inheritdoc/>
    public T Value { get; }

    /// <inheritdoc/>
    public bool IsSuccess { get; }

    /// <inheritdoc/>
    public bool IsNotFound { get; }

    /// <inheritdoc/>
    public bool IsRateLimited { get; }

    /// <inheritdoc/>
    public IReadOnlyList<IDeliveryError> Errors { get; }

    /// <inheritdoc/>
    public IRateLimitInfo? RateLimit { get; }

    /// <inheritdoc/>
    public int StatusCode { get; }

    /// <inheritdoc/>
    public bool HasStaleContent { get; }

    /// <inheritdoc/>
    public string? ContinuationToken { get; }

    /// <inheritdoc/>
    public string? RequestUrl { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The result value.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="hasStaleContent">Whether the content is stale.</param>
    /// <param name="continuationToken">The continuation token for pagination.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="rateLimit">Rate limiting information.</param>
    internal DeliveryResult(
        T value,
        int statusCode = 200,
        bool hasStaleContent = false,
        string? continuationToken = null,
        string? requestUrl = null,
        IRateLimitInfo? rateLimit = null)
    {
        Value = value;
        IsSuccess = true;
        IsNotFound = false;
        IsRateLimited = false;
        Errors = Array.Empty<IDeliveryError>();
        StatusCode = statusCode;
        HasStaleContent = hasStaleContent;
        ContinuationToken = continuationToken;
        RequestUrl = requestUrl;
        RateLimit = rateLimit;
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errors">The errors that occurred.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="rateLimit">Rate limiting information.</param>
    internal DeliveryResult(
        IReadOnlyList<IDeliveryError> errors,
        int statusCode,
        string? requestUrl = null,
        IRateLimitInfo? rateLimit = null)
    {
        Value = default!;
        IsSuccess = false;
        IsNotFound = statusCode == (int)HttpStatusCode.NotFound;
        IsRateLimited = statusCode == (int)HttpStatusCode.TooManyRequests;
        Errors = errors;
        StatusCode = statusCode;
        HasStaleContent = false;
        ContinuationToken = null;
        RequestUrl = requestUrl;
        RateLimit = rateLimit;
    }

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error that occurred.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="rateLimit">Rate limiting information.</param>
    internal DeliveryResult(
        IDeliveryError error,
        int statusCode,
        string? requestUrl = null,
        IRateLimitInfo? rateLimit = null)
        : this(new[] { error }, statusCode, requestUrl, rateLimit)
    {
    }
}

/// <summary>
/// Factory methods for creating <see cref="IDeliveryResult{T}"/> instances.
/// </summary>
internal static class DeliveryResult
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="value">The result value.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="hasStaleContent">Whether the content is stale.</param>
    /// <param name="continuationToken">The continuation token for pagination.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="rateLimit">Rate limiting information.</param>
    /// <returns>A successful result.</returns>
    public static IDeliveryResult<T> Success<T>(
        T value,
        int statusCode = 200,
        bool hasStaleContent = false,
        string? continuationToken = null,
        string? requestUrl = null,
        IRateLimitInfo? rateLimit = null)
    {
        return new DeliveryResult<T>(value, statusCode, hasStaleContent, continuationToken, requestUrl, rateLimit);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="errors">The errors that occurred.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="rateLimit">Rate limiting information.</param>
    /// <returns>A failed result.</returns>
    public static IDeliveryResult<T> Failure<T>(
        IReadOnlyList<IDeliveryError> errors,
        int statusCode,
        string? requestUrl = null,
        IRateLimitInfo? rateLimit = null)
    {
        return new DeliveryResult<T>(errors, statusCode, requestUrl, rateLimit);
    }

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="requestId">The request ID.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="rateLimit">Rate limiting information.</param>
    /// <returns>A failed result.</returns>
    public static IDeliveryResult<T> Failure<T>(
        string message,
        int statusCode,
        string? errorCode = null,
        string? requestId = null,
        string? requestUrl = null,
        IRateLimitInfo? rateLimit = null)
    {
        var error = new DeliveryError(message, errorCode, requestId);
        return new DeliveryResult<T>(error, statusCode, requestUrl, rateLimit);
    }

    /// <summary>
    /// Creates a "not found" result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="message">The error message.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <returns>A not found result.</returns>
    public static IDeliveryResult<T> NotFound<T>(string message, string? requestUrl = null)
    {
        return Failure<T>(message, (int)HttpStatusCode.NotFound, requestUrl: requestUrl);
    }

    /// <summary>
    /// Creates a "rate limited" result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="message">The error message.</param>
    /// <param name="rateLimit">Rate limiting information.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <returns>A rate limited result.</returns>
    public static IDeliveryResult<T> RateLimited<T>(
        string message,
        IRateLimitInfo? rateLimit = null,
        string? requestUrl = null)
    {
        return Failure<T>(message, (int)HttpStatusCode.TooManyRequests, requestUrl: requestUrl, rateLimit: rateLimit);
    }
}

/// <summary>
/// Concrete implementation of <see cref="IRateLimitInfo"/>.
/// </summary>
internal sealed class RateLimitInfo : IRateLimitInfo
{
    /// <inheritdoc/>
    public int? Remaining { get; }

    /// <inheritdoc/>
    public int? Limit { get; }

    /// <inheritdoc/>
    public DateTimeOffset? Reset { get; }

    /// <inheritdoc/>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitInfo"/> class.
    /// </summary>
    /// <param name="remaining">The number of requests remaining.</param>
    /// <param name="limit">The total number of requests allowed.</param>
    /// <param name="reset">When the rate limit window resets.</param>
    /// <param name="retryAfter">The retry-after duration.</param>
    public RateLimitInfo(int? remaining, int? limit, DateTimeOffset? reset, TimeSpan? retryAfter)
    {
        Remaining = remaining;
        Limit = limit;
        Reset = reset;
        RetryAfter = retryAfter;
    }
}

/// <summary>
/// Concrete implementation of <see cref="IDeliveryError"/>.
/// </summary>
internal sealed class DeliveryError : IDeliveryError
{
    /// <inheritdoc/>
    public string Message { get; }

    /// <inheritdoc/>
    public string? ErrorCode { get; }

    /// <inheritdoc/>
    public string? RequestId { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object>? Details { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryError"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="requestId">The request ID.</param>
    /// <param name="details">Additional error details.</param>
    public DeliveryError(
        string message,
        string? errorCode = null,
        string? requestId = null,
        IReadOnlyDictionary<string, object>? details = null)
    {
        Message = message;
        ErrorCode = errorCode;
        RequestId = requestId;
        Details = details;
    }
}