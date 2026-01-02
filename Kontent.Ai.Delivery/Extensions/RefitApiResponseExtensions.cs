using System.Net;

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
        if (apiResponse.IsSuccessful && apiResponse.Content is not null)
        {
            return Task.FromResult(DeliveryResult.Success(
                value: apiResponse.Content,
                requestUrl: apiResponse.RequestMessage?.RequestUri?.ToString() ?? string.Empty,
                statusCode: apiResponse.StatusCode,
                hasStaleContent: ExtractHasStaleContent(apiResponse),
                continuationToken: ExtractContinuationToken(apiResponse),
                responseHeaders: apiResponse.Headers
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
    private static Task<IDeliveryResult<T>> MapFailureAsync<T>(IApiResponse<T> apiResponse)
    {
        var url = apiResponse.RequestMessage?.RequestUri?.ToString() ?? string.Empty;
        var status = apiResponse.StatusCode;
        var headers = apiResponse.Headers;

        if (apiResponse.Error is not ApiException apiEx)
        {
            var fallback = new Error { Message = "Unknown error", ErrorCode = (int)status };
            return Task.FromResult(DeliveryResult.Failure<T>(url, status, fallback, headers));
        }

        return MapApiExceptionAsync(apiEx, url, status, headers);

        static async Task<IDeliveryResult<T>> MapApiExceptionAsync(
            ApiException ex,
            string url,
            HttpStatusCode status,
            System.Net.Http.Headers.HttpResponseHeaders? headers)
        {
            Error error;
            try
            {
                // Try to parse a structured Kontent API error from the body.
                var parsed = await ex.GetContentAsAsync<Error>().ConfigureAwait(false);
                error = parsed ?? new Error { Message = ex.Message, ErrorCode = (int)status };
            }
            catch
            {
                // Body isn't JSON or deserialization failed – fall back to best available message.
                error = new Error { Message = ex.InnerException?.Message ?? ex.Message, ErrorCode = (int)status };
            }

            return DeliveryResult.Failure<T>(url, status, error, headers);
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