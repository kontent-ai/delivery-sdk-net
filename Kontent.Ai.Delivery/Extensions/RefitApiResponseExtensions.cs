using System.Net;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

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
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    /// <returns>A delivery result containing the response data or errors.</returns>
    public static Task<IDeliveryResult<T>> ToDeliveryResultAsync<T>(this IApiResponse<T> apiResponse, ILogger? logger = null)
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
        return MapFailureAsync(apiResponse, logger);
    }

    /// <summary>
    /// Maps a failure response to a delivery result.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="apiResponse">The API response.</param>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    /// <returns>A delivery result containing the response data or errors.</returns>
    private static Task<IDeliveryResult<T>> MapFailureAsync<T>(IApiResponse<T> apiResponse, ILogger? logger)
    {
        var url = apiResponse.RequestMessage?.RequestUri?.ToString() ?? string.Empty;
        var status = apiResponse.StatusCode;
        var headers = apiResponse.Headers;

        if (apiResponse.Error is not ApiException apiEx)
        {
            var fallback = new Error
            {
                Message = "Unknown error",
                ErrorCode = (int)status,
                Exception = apiResponse.Error
            };
            return Task.FromResult(DeliveryResult.Failure<T>(url, status, fallback, headers));
        }

        return MapApiExceptionAsync(apiEx, url, status, headers, logger);

        static async Task<IDeliveryResult<T>> MapApiExceptionAsync(
            ApiException ex,
            string url,
            HttpStatusCode status,
            System.Net.Http.Headers.HttpResponseHeaders? headers,
            ILogger? logger)
        {
            Error error;
            try
            {
                // Try to parse a structured Kontent API error from the body.
                var parsed = await ex.GetContentAsAsync<Error>().ConfigureAwait(false);
                if (parsed is not null)
                {
                    // Preserve the exception in the parsed error
                    error = parsed with { Exception = ex };
                }
                else
                {
                    error = new Error { Message = ex.Message, ErrorCode = (int)status, Exception = ex };
                }
            }
            catch (Exception parseEx)
            {
                // Log the deserialization failure for diagnostics
                if (logger != null)
                {
                    LoggerMessages.ApiErrorParsingFailed(logger, url, status, ex.Content?.Length ?? 0, parseEx);
                }

                // Body isn't JSON or deserialization failed.
                // Use Refit's formatted message as the base (includes HTTP context),
                // and append the raw response body for debugging if available.
                var rawBody = ex.Content;
                string message;

                if (!string.IsNullOrWhiteSpace(rawBody))
                {
                    // Truncate very long responses (e.g., HTML error pages) to keep the message readable
                    const int maxBodyLength = 500;
                    var truncatedBody = rawBody.Length > maxBodyLength
                        ? rawBody[..maxBodyLength] + "... (truncated)"
                        : rawBody;

                    message = $"{ex.Message} | Raw response: {truncatedBody}";
                }
                else
                {
                    message = ex.Message;
                }

                error = new Error { Message = message, ErrorCode = (int)status, Exception = ex };
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
