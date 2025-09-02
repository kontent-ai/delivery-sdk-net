using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions.Serialization;
using Kontent.Ai.Delivery.Abstractions.SharedModels;
using Kontent.Ai.Delivery.SharedModels;
using Refit;

namespace Kontent.Ai.Delivery.Extensions;

/// <summary>
/// Extension methods for converting Refit's IApiResponse to DeliveryResult.
/// </summary>
internal static class RefitApiResponseExtensions
{
    private const string ContinuationHeaderName = "X-Continuation";
    private const string StaleContentHeaderName = "X-Stale-Content";

    /// <summary>
    /// Converts a Refit API response to a Delivery result.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="apiResponse">The Refit API response.</param>
    /// <returns>A delivery result containing the response data or errors.</returns>
    public static Task<IDeliveryResult<T>> ToDeliveryResultAsync<T>(this IApiResponse<T> apiResponse)
    {
        // Fast success path: no awaits/allocations
        if (apiResponse.IsSuccessStatusCode && apiResponse.Content is not null)
        {
            return Task.FromResult(DeliveryResult.Success(
                value: apiResponse.Content,
                requestUrl: apiResponse.RequestMessage?.RequestUri?.ToString() ?? string.Empty,
                statusCode: (int)apiResponse.StatusCode,
                hasStaleContent: ExtractHasStaleContent(apiResponse),
                continuationToken: ExtractContinuationToken(apiResponse)
            ));
        }

        // Defer to the async failure/edge handler
        return MapFailureAsync(apiResponse);
    }

    /// <summary>
    /// Maps a failure response to a delivery result.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="apiResponse">The API response.</param>
    /// <returns>A delivery result containing the response data or errors.</returns>
    private static async Task<IDeliveryResult<T>> MapFailureAsync<T>(IApiResponse<T> apiResponse)
    {
        var url = apiResponse.RequestMessage?.RequestUri?.ToString() ?? string.Empty;
        var status = (int)apiResponse.StatusCode;

        var error =
            apiResponse.Error is ApiException ex
                ? await TryGetErrorAsync(ex, status)
                : new Error { Message = "Unknown error", ErrorCode = status };

        return DeliveryResult.Failure<T>(
            requestUrl: url,
            statusCode: status,
            error: error);

        static async Task<Error> TryGetErrorAsync(ApiException ex, int status)
        {
            try
            {
                // Let Refit deserialize the error body
                var parsed = await ex.GetContentAsAsync<Error>();
                return parsed ?? new Error { Message = ex.Message, ErrorCode = status };
            }
            catch
            {
                // Fallback when the body isn’t JSON or deserialization fails
                var raw = ex.Content;
                return !string.IsNullOrWhiteSpace(raw)
                    ? new Error { Message = raw, ErrorCode = status }
                    : new Error { Message = ex.Message, ErrorCode = status };
            }
        }
    }



    /// <summary>
    /// Extracts continuation token from response headers.
    /// </summary>
    /// <param name="apiResponse">The API response.</param>
    /// <returns>The continuation token if present.</returns>
    private static string? ExtractContinuationToken<T>(IApiResponse<T> apiResponse)
    {
        if (apiResponse.Headers?.TryGetValues(ContinuationHeaderName, out var continuationValues) == true)
        {
            return continuationValues.FirstOrDefault();
        }

        return null;
    }

    /// <summary>
    /// Extracts stale content indicator from response headers.
    /// </summary>
    /// <param name="apiResponse">The API response.</param>
    /// <returns>True if content is stale.</returns>
    private static bool ExtractHasStaleContent<T>(IApiResponse<T> apiResponse)
    {
        if (apiResponse.Headers?.TryGetValues(StaleContentHeaderName, out var staleValues) == true)
        {
            return staleValues.FirstOrDefault()?.Equals("1", StringComparison.OrdinalIgnoreCase) == true;
        }

        return false;
    }

    /// <summary>
    /// Extracts continuation token from response headers.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="response">The API response.</param>
    /// <returns>The continuation token if present.</returns>
    internal static string? Continuation<T>(this IApiResponse<T> response)
        => response.Headers.TryGetValues(ContinuationHeaderName, out var vals) ? vals.FirstOrDefault() : null;
}