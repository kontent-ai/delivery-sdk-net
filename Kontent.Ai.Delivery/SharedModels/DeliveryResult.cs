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
    public string? RequestUrl { get; }

    /// <inheritdoc/>
    public HttpResponseHeaders? ResponseHeaders { get; }

    /// <inheritdoc/>
    public ResponseSource ResponseSource { get; }

    /// <inheritdoc/>
    public bool IsCacheHit => ResponseSource is ResponseSource.Cache or ResponseSource.FailSafe;

    /// <inheritdoc/>
    public IReadOnlyList<string>? DependencyKeys { get; }

    /// <summary>
    /// Creates a successful result from an API response.
    /// </summary>
    internal DeliveryResult(
        T value,
        string requestUrl,
        HttpStatusCode statusCode,
        bool hasStaleContent,
        HttpResponseHeaders? responseHeaders,
        ResponseSource responseSource,
        IReadOnlyList<string>? dependencyKeys = null)
    {
        Value = value;
        IsSuccess = true;
        StatusCode = statusCode;
        HasStaleContent = hasStaleContent;
        RequestUrl = requestUrl;
        ResponseHeaders = responseHeaders;
        ResponseSource = responseSource;
        DependencyKeys = dependencyKeys;
    }

    /// <summary>
    /// Creates a successful result from SDK cache.
    /// </summary>
    internal DeliveryResult(
        T value,
        ResponseSource responseSource,
        IReadOnlyList<string>? dependencyKeys = null)
    {
        Value = value;
        IsSuccess = true;
        StatusCode = HttpStatusCode.OK;
        HasStaleContent = false;
        RequestUrl = null;
        ResponseHeaders = null;
        ResponseSource = responseSource;
        DependencyKeys = dependencyKeys;
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    internal DeliveryResult(
        string requestUrl,
        HttpStatusCode statusCode,
        IError? error,
        HttpResponseHeaders? responseHeaders,
        ResponseSource responseSource = ResponseSource.Origin,
        IReadOnlyList<string>? dependencyKeys = null)
    {
        Value = default!;
        IsSuccess = false;
        Error = error;
        StatusCode = statusCode;
        HasStaleContent = false;
        RequestUrl = requestUrl;
        ResponseHeaders = responseHeaders;
        ResponseSource = responseSource;
        DependencyKeys = dependencyKeys;
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
    public static IDeliveryResult<T> Success<T>(
        T value,
        string requestUrl,
        HttpStatusCode statusCode,
        bool hasStaleContent,
        HttpResponseHeaders? responseHeaders,
        ResponseSource responseSource,
        IReadOnlyList<string>? dependencyKeys = null)
    => new DeliveryResult<T>(
        value,
        requestUrl,
        statusCode,
        hasStaleContent,
        responseHeaders,
        responseSource,
        dependencyKeys);

    /// <summary>
    /// Creates a successful result by projecting metadata from another delivery result.
    /// </summary>
    public static IDeliveryResult<TOut> SuccessFrom<TOut, TIn>(
        TOut value,
        IDeliveryResult<TIn> source,
        IReadOnlyList<string>? dependencyKeys = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        return Success(
            value,
            source.RequestUrl ?? string.Empty,
            source.StatusCode,
            source.HasStaleContent,
            source.ResponseHeaders,
            source.ResponseSource,
            dependencyKeys);
    }

    /// <summary>
    /// Creates a successful result from SDK cache.
    /// </summary>
    public static IDeliveryResult<T> CacheHit<T>(
        T value,
        IReadOnlyList<string>? dependencyKeys = null)
    => new DeliveryResult<T>(value, ResponseSource.Cache, dependencyKeys);

    /// <summary>
    /// Creates a successful result from SDK cache fail-safe (stale data served after factory failure).
    /// </summary>
    public static IDeliveryResult<T> FailSafeHit<T>(
        T value,
        IReadOnlyList<string>? dependencyKeys = null)
    => new DeliveryResult<T>(value, ResponseSource.FailSafe, dependencyKeys);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static IDeliveryResult<T> Failure<T>(
        string requestUrl,
        HttpStatusCode statusCode,
        IError? error,
        HttpResponseHeaders? responseHeaders = null,
        ResponseSource responseSource = ResponseSource.Origin)
    => new DeliveryResult<T>(requestUrl, statusCode, error, responseHeaders, responseSource);

    /// <summary>
    /// Creates a failed result by projecting failure metadata from another delivery result.
    /// </summary>
    public static IDeliveryResult<TOut> FailureFrom<TOut, TIn>(IDeliveryResult<TIn> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return Failure<TOut>(
            source.RequestUrl ?? string.Empty,
            source.StatusCode,
            source.Error,
            source.ResponseHeaders,
            source.ResponseSource);
    }
}
