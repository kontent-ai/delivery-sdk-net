using System;
using System.Net.Http;
using System.Threading.Tasks;
using Refit;
using System.Linq;

namespace Kontent.Ai.Delivery.Extensions;

/// <summary>
/// Extension methods for the Refit's ApiResponse class.
/// </summary>
internal static class ApiResponseExtensions
{
    private const string ContinuationHeaderName = "X-Continuation";
    private const string StaleHeaderName = "X-Stale-Content";

    /// <summary>
    /// Gets the continuation token from the response headers.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="response">The response to get the continuation token from.</param>
    /// <returns>The continuation token or null if not found.</returns>
    public static string? GetContinuationToken<T>(this ApiResponse<T> response)
        => response.Headers.TryGetValues(ContinuationHeaderName, out var values)
            ? values.FirstOrDefault()
            : null;

    /// <summary>
    /// Checks if the response has stale content.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="response">The response to check for stale content.</param>
    /// <returns>True if the response has stale content, false otherwise.</returns>
    public static bool HasStaleContent<T>(this ApiResponse<T> response)
        => response.Headers.TryGetValues(StaleHeaderName, out var values)
            && values.Contains("1", StringComparer.Ordinal);

    /// <summary>
    /// Gets the request URL from the response.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="response">The response to get the request URL from.</param>
    /// <returns>The request URL or null if not found.</returns>
    public static string? GetRequestUrl<T>(this ApiResponse<T> response)
        => response.RequestMessage?.RequestUri?.ToString();

    /// <summary>
    /// Checks if the response has a success status code.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="response">The response to check for a success status code.</param>
    /// <returns>True if the response has a success status code, false otherwise.</returns>
    public static bool IsSuccessStatusCode<T>(this ApiResponse<T> response)
        => response.IsSuccessStatusCode;

    /// <summary>
    /// Gets the raw content of the response.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="response">The response to get the raw content from.</param>
    /// <returns>The raw content or null if not found.</returns>
    public static Task<string?> GetRawContentAsync<T>(this ApiResponse<T> response)
    {
        if (response.Content != null)
        {
            return Task.FromResult<string?>(System.Text.Json.JsonSerializer.Serialize(response.Content));
        }
        if (response.Error?.Content != null)
        {
            return Task.FromResult<string?>(response.Error.Content);
        }
        return Task.FromResult<string?>(null);
    }
}


