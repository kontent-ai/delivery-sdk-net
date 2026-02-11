using System.Net;
using System.Net.Http.Headers;

namespace Kontent.Ai.Delivery.SharedModels;

/// <summary>
/// Concrete implementation of <see cref="IDeliveryResult{T}"/>.
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
    public HttpStatusCode StatusCode { get; }

    /// <inheritdoc/>
    public bool HasStaleContent { get; }

    /// <inheritdoc/>
    public string? ContinuationToken { get; }

    /// <inheritdoc/>
    public string? RequestUrl { get; }

    /// <inheritdoc/>
    public HttpResponseHeaders? ResponseHeaders { get; }

    /// <inheritdoc/>
    public bool IsCacheHit { get; }

    /// <summary>
    /// Creates a successful result from an API response.
    /// </summary>
    /// <param name="value">The result value.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="hasStaleContent">Whether the content is stale.</param>
    /// <param name="continuationToken">The continuation token for pagination.</param>
    /// <param name="responseHeaders">The HTTP response headers.</param>
    internal DeliveryResult(
        T value,
        string requestUrl,
        HttpStatusCode statusCode,
        bool hasStaleContent,
        string? continuationToken,
        HttpResponseHeaders? responseHeaders)
    {
        Value = value;
        IsSuccess = true;
        StatusCode = statusCode;
        HasStaleContent = hasStaleContent;
        ContinuationToken = continuationToken;
        RequestUrl = requestUrl;
        ResponseHeaders = responseHeaders;
        IsCacheHit = false;
    }

    /// <summary>
    /// Creates a successful result from SDK cache.
    /// </summary>
    /// <param name="value">The cached value.</param>
    internal DeliveryResult(T value)
    {
        Value = value;
        IsSuccess = true;
        StatusCode = HttpStatusCode.OK;
        HasStaleContent = false;
        ContinuationToken = null;
        RequestUrl = null;
        ResponseHeaders = null;
        IsCacheHit = true;
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="error">The error that occurred.</param>
    /// <param name="responseHeaders">The HTTP response headers.</param>
    internal DeliveryResult(
        string requestUrl,
        HttpStatusCode statusCode,
        IError? error,
        HttpResponseHeaders? responseHeaders)
    {
        Value = default!;
        IsSuccess = false;
        Error = error;
        StatusCode = statusCode;
        HasStaleContent = false;
        ContinuationToken = null;
        RequestUrl = requestUrl;
        ResponseHeaders = responseHeaders;
        IsCacheHit = false;
    }
}

/// <summary>
/// Factory methods for creating <see cref="IDeliveryResult{T}"/> instances.
/// </summary>
internal static class DeliveryResult
{
    /// <summary>
    /// Creates a successful result from an API response.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="value">The result value.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="hasStaleContent">Whether the content is stale.</param>
    /// <param name="continuationToken">The continuation token for pagination.</param>
    /// <param name="responseHeaders">The HTTP response headers.</param>
    /// <returns>A successful result.</returns>
    public static IDeliveryResult<T> Success<T>(
        T value,
        string requestUrl,
        HttpStatusCode statusCode,
        bool hasStaleContent,
        string? continuationToken,
        HttpResponseHeaders? responseHeaders)
    => new DeliveryResult<T>(value, requestUrl, statusCode, hasStaleContent, continuationToken, responseHeaders);

    /// <summary>
    /// Creates a successful result by projecting metadata from another delivery result.
    /// </summary>
    /// <typeparam name="TOut">The output result type.</typeparam>
    /// <typeparam name="TIn">The source result type.</typeparam>
    /// <param name="value">The output result value.</param>
    /// <param name="source">The source result to copy metadata from.</param>
    /// <returns>A successful result preserving source metadata.</returns>
    public static IDeliveryResult<TOut> SuccessFrom<TOut, TIn>(TOut value, IDeliveryResult<TIn> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return Success(
            value,
            source.RequestUrl ?? string.Empty,
            source.StatusCode,
            source.HasStaleContent,
            source.ContinuationToken,
            source.ResponseHeaders);
    }

    /// <summary>
    /// Creates a successful result from SDK cache.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="value">The cached value.</param>
    /// <returns>A successful cache hit result.</returns>
    public static IDeliveryResult<T> CacheHit<T>(T value)
    => new DeliveryResult<T>(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="error">The error that occurred.</param>
    /// <param name="responseHeaders">The HTTP response headers.</param>
    /// <returns>A failed result.</returns>
    public static IDeliveryResult<T> Failure<T>(
        string requestUrl,
        HttpStatusCode statusCode,
        IError? error,
        HttpResponseHeaders? responseHeaders = null)
    => new DeliveryResult<T>(requestUrl, statusCode, error, responseHeaders);

    /// <summary>
    /// Creates a failed result by projecting failure metadata from another delivery result.
    /// </summary>
    /// <typeparam name="TOut">The output result type.</typeparam>
    /// <typeparam name="TIn">The source result type.</typeparam>
    /// <param name="source">The source result to copy metadata from.</param>
    /// <returns>A failed result preserving source metadata.</returns>
    public static IDeliveryResult<TOut> FailureFrom<TOut, TIn>(IDeliveryResult<TIn> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return Failure<TOut>(
            source.RequestUrl ?? string.Empty,
            source.StatusCode,
            source.Error,
            source.ResponseHeaders);
    }
}
