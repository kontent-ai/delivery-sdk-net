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
    public IError? Error { get; }

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
    internal DeliveryResult(
        T value,
        string requestUrl,
        int statusCode = 200,
        bool hasStaleContent = false,
        string? continuationToken = null)
    {
        Value = value;
        IsSuccess = true;
        StatusCode = statusCode;
        HasStaleContent = hasStaleContent;
        ContinuationToken = continuationToken;
        RequestUrl = requestUrl;
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="error">The error that occurred.</param>
    internal DeliveryResult(
        string requestUrl,
        int statusCode,
        IError? error)
    {
        Value = default!;
        IsSuccess = false;
        Error = error;
        StatusCode = statusCode;
        HasStaleContent = false;
        ContinuationToken = null;
        RequestUrl = requestUrl;
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
    /// <returns>A successful result.</returns>
    public static IDeliveryResult<T> Success<T>(
        T value,
        string requestUrl,
        int statusCode = 200,
        bool hasStaleContent = false,
        string? continuationToken = null)
    => new DeliveryResult<T>(value, requestUrl, statusCode, hasStaleContent, continuationToken);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="error">The error that occurred.</param>
    /// <returns>A failed result.</returns>
    public static IDeliveryResult<T> Failure<T>(
        string requestUrl,
        int statusCode,
        IError? error)
    => new DeliveryResult<T>(requestUrl, statusCode, error);
}