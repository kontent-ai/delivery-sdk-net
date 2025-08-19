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
    /// <summary>
    /// Converts a Refit API response to a Delivery result.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="apiResponse">The Refit API response.</param>
    /// <param name="jsonSerializer">The JSON serializer for error parsing.</param>
    /// <returns>A delivery result containing the response data or errors.</returns>
    public static async Task<IDeliveryResult<T>> ToDeliveryResultAsync<T>(
        this IApiResponse<T> apiResponse,
        IJsonSerializer? jsonSerializer = null)
    {
        if (apiResponse.IsSuccessStatusCode && apiResponse.Content is not null)
        {
            return DeliveryResult.Success(
                apiResponse.Content,
                (int)apiResponse.StatusCode,
                hasStaleContent: ExtractHasStaleContent(apiResponse),
                continuationToken: ExtractContinuationToken(apiResponse),
                requestUrl: apiResponse.RequestMessage?.RequestUri?.ToString(),
                rateLimit: ExtractRateLimitInfo(apiResponse));
        }

        // Handle error response
        var errors = await ExtractErrorsAsync(apiResponse, jsonSerializer);
        
        return DeliveryResult.Failure<T>(
            errors,
            (int)apiResponse.StatusCode,
            requestUrl: apiResponse.RequestMessage?.RequestUri?.ToString(),
            rateLimit: ExtractRateLimitInfo(apiResponse));
    }

    /// <summary>
    /// Extracts continuation token from response headers.
    /// </summary>
    /// <param name="apiResponse">The API response.</param>
    /// <returns>The continuation token if present.</returns>
    private static string? ExtractContinuationToken<T>(IApiResponse<T> apiResponse)
    {
        if (apiResponse.Headers?.TryGetValues("X-Continuation", out var continuationValues) == true)
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
        if (apiResponse.Headers?.TryGetValues("X-Stale-Content", out var staleValues) == true)
        {
            return staleValues.FirstOrDefault()?.Equals("1", StringComparison.OrdinalIgnoreCase) == true;
        }

        return false;
    }

    /// <summary>
    /// Extracts rate limiting information from response headers.
    /// </summary>
    /// <param name="apiResponse">The API response.</param>
    /// <returns>Rate limiting information if available.</returns>
    private static IRateLimitInfo? ExtractRateLimitInfo<T>(IApiResponse<T> apiResponse)
    {
        var headers = apiResponse.Headers;
        if (headers == null) return null;

        // Extract rate limit headers
        var remaining = ExtractIntHeader(headers, "X-RateLimit-Remaining");
        var limit = ExtractIntHeader(headers, "X-RateLimit-Limit");
        
        DateTimeOffset? reset = null;
        if (headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
        {
            var resetValue = resetValues.FirstOrDefault();
            if (long.TryParse(resetValue, out var resetUnix))
            {
                reset = DateTimeOffset.FromUnixTimeSeconds(resetUnix);
            }
        }

        TimeSpan? retryAfter = null;
        if (headers.TryGetValues("Retry-After", out var retryValues))
        {
            var retryValue = retryValues.FirstOrDefault();
            if (int.TryParse(retryValue, out var retrySeconds))
            {
                retryAfter = TimeSpan.FromSeconds(retrySeconds);
            }
        }

        // Only create RateLimitInfo if we have at least one piece of information
        if (remaining.HasValue || limit.HasValue || reset.HasValue || retryAfter.HasValue)
        {
            return new RateLimitInfo(remaining, limit, reset, retryAfter);
        }

        return null;
    }

    /// <summary>
    /// Extracts an integer value from response headers.
    /// </summary>
    /// <param name="headers">The response headers.</param>
    /// <param name="headerName">The header name.</param>
    /// <returns>The integer value if present and valid.</returns>
    private static int? ExtractIntHeader(HttpResponseHeaders headers, string headerName)
    {
        if (headers.TryGetValues(headerName, out var values))
        {
            var value = values.FirstOrDefault();
            if (int.TryParse(value, out var intValue))
            {
                return intValue;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts error information from the API response.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="apiResponse">The API response.</param>
    /// <param name="jsonSerializer">The JSON serializer for error parsing.</param>
    /// <returns>A list of delivery errors.</returns>
    private static async Task<IReadOnlyList<IDeliveryError>> ExtractErrorsAsync<T>(
        IApiResponse<T> apiResponse,
        IJsonSerializer? jsonSerializer)
    {
        var errors = new List<IDeliveryError>();

        // Try to extract error from response content if available
        if (apiResponse.Error?.Content != null)
        {
            try
            {
                if (jsonSerializer != null)
                {
                    // Try to parse structured error response from Kontent.ai
                    var errorResponse = jsonSerializer.Deserialize<KontentErrorResponse>(apiResponse.Error.Content);
                    if (errorResponse?.Message != null)
                    {
                        errors.Add(new DeliveryError(
                            errorResponse.Message,
                            errorResponse.ErrorCode?.ToString(),
                            errorResponse.RequestId));
                    }
                }
            }
            catch
            {
                // If structured parsing fails, fall back to raw content
                errors.Add(new DeliveryError(apiResponse.Error.Content));
            }
        }

        // If no specific error content, create a generic error based on status code
        if (errors.Count == 0)
        {
            var message = apiResponse.StatusCode switch
            {
                HttpStatusCode.NotFound => "Content not found",
                HttpStatusCode.Unauthorized => "Unauthorized access",
                HttpStatusCode.Forbidden => "Access forbidden",
                HttpStatusCode.TooManyRequests => "Rate limit exceeded",
                HttpStatusCode.InternalServerError => "Internal server error",
                HttpStatusCode.BadGateway => "Bad gateway",
                HttpStatusCode.ServiceUnavailable => "Service unavailable",
                HttpStatusCode.GatewayTimeout => "Gateway timeout",
                _ => $"Request failed with status {(int)apiResponse.StatusCode}: {apiResponse.StatusCode}"
            };

            errors.Add(new DeliveryError(message));
        }

        return errors;
    }

    /// <summary>
    /// Represents the structure of error responses from Kontent.ai Delivery API.
    /// </summary>
    private sealed class KontentErrorResponse
    {
        public string? Message { get; set; }
        public string? RequestId { get; set; }
        public int? ErrorCode { get; set; }
        public int? SpecificCode { get; set; }
    }
}