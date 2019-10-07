using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Kentico.Kontent.Delivery.Extensions;

namespace Kentico.Kontent.Delivery.RetryPolicy
{
    internal class DefaultRetryPolicy : IRetryPolicy
    {
        private static readonly Random Random = new Random();
        private static readonly WebExceptionStatus[] WebExceptionStatusesToRetry =
        {
            WebExceptionStatus.ConnectFailure,
            WebExceptionStatus.ConnectionClosed,
            WebExceptionStatus.KeepAliveFailure,
            WebExceptionStatus.NameResolutionFailure,
            WebExceptionStatus.ReceiveFailure,
            WebExceptionStatus.SendFailure,
            WebExceptionStatus.Timeout
        };
        private static readonly HttpStatusCode[] StatusCodesToRetry =
        {
            HttpStatusCode.RequestTimeout,
            (HttpStatusCode)429, // Too Many Requests
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout,
        };
        private static readonly HttpStatusCode[] StatusCodesWithPossibleRetryHeader =
        {
            (HttpStatusCode)429, // Too Many Requests
            HttpStatusCode.ServiceUnavailable,
        };

        private readonly DefaultRetryPolicyOptions _options;

        public DefaultRetryPolicy(DefaultRetryPolicyOptions options)
        {
            _options = options;
        }

        public async Task<HttpResponseMessage> ExecuteAsync(Func<Task<HttpResponseMessage>> sendRequest)
        {
            var waitTime = TimeSpan.Zero;
            var cumulativeWaitTime = TimeSpan.Zero;

            for (var retryAttempts = 0; ; ++retryAttempts, cumulativeWaitTime += waitTime)
            {
                if (waitTime > TimeSpan.Zero)
                {
                    await Task.Delay(waitTime);
                }

                try
                {
                    var response = await sendRequest();
                    var shouldRetry = ShouldRetry(response);

                    if (shouldRetry)
                    {
                        waitTime = StatusCodesWithPossibleRetryHeader.Contains(response.StatusCode) && response.Headers.TryGetRetryHeader(out var retryAfter) && retryAfter > TimeSpan.Zero
                            ? retryAfter
                            : GetNextWaitTime(retryAttempts);
                    }

                    if (!shouldRetry || cumulativeWaitTime + waitTime > _options.MaxCumulativeWaitTime)
                    {
                        return response;
                    }
                }
                catch (Exception e)
                {
                    var shouldRetry = ShouldRetry(e);

                    if (shouldRetry)
                    {
                        waitTime = GetNextWaitTime(retryAttempts);
                    }

                    if (!shouldRetry || cumulativeWaitTime + waitTime > _options.MaxCumulativeWaitTime)
                    {
                        throw;
                    }
                }
            }
        }

        private TimeSpan GetNextWaitTime(int retryAttempts)
            => TimeSpan.FromMilliseconds(Random.Next(Convert.ToInt32(0.8 * _options.DeltaBackoff.TotalMilliseconds), Convert.ToInt32(1.2 * _options.DeltaBackoff.TotalMilliseconds)) * (int)Math.Pow(2, retryAttempts));

        private static bool ShouldRetry(Exception exception) => exception?.InnerException is WebException we && WebExceptionStatusesToRetry.Contains(we.Status);

        private static bool ShouldRetry(HttpResponseMessage response) => StatusCodesToRetry.Contains(response.StatusCode);
    }
}