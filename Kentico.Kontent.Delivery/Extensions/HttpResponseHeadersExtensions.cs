using System;
using System.Linq;
using System.Net.Http.Headers;

namespace Kentico.Kontent.Delivery.Extensions
{
    internal static class HttpResponseHeadersExtensions
    {
        private const string ContinuationHeaderName = "X-Continuation";

        internal static string GetContinuationHeader(this HttpResponseHeaders headers)
        {
            return headers.TryGetValues(ContinuationHeaderName, out var headerValues)
                ? headerValues.FirstOrDefault()
                : null;
        }

        internal static bool TryGetRetryHeader(this HttpResponseHeaders headers, out TimeSpan retryAfter)
        {
            TimeSpan GetPositiveOrZero(TimeSpan timeSpan) => timeSpan < TimeSpan.Zero ? TimeSpan.Zero : timeSpan;

            if (headers?.RetryAfter?.Date != null)
            {
                retryAfter = GetPositiveOrZero(headers.RetryAfter.Date.Value - DateTime.UtcNow);
                return true;
            }

            if (headers?.RetryAfter?.Delta != null)
            {
                retryAfter = GetPositiveOrZero(headers.RetryAfter.Delta.GetValueOrDefault(TimeSpan.Zero));
                return true;
            }

            return false;
        }
    }
}
