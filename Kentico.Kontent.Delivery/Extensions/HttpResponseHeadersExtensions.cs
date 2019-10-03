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
    }
}
