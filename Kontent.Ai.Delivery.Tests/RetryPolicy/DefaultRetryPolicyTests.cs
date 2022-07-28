using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.RetryPolicy;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.RetryPolicy
{
    public class DefaultRetryPolicyTests
    {
        [Fact]
        public async Task ExecuteAsync_ResponseOk_DoesNotRetry()
        {
            var options = new DefaultRetryPolicyOptions
            {
                DeltaBackoff = TimeSpan.FromSeconds(5)
            };
            var retryPolicy = new DefaultRetryPolicy(options);
            var client = new FakeSender().Returns(HttpStatusCode.OK);

            var response = await retryPolicy.ExecuteAsync(client.SendRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, client.TimesCalled);
        }

        [Fact]
        public async Task ExecuteAsync_RecoversAfterNotSuccessStatusCode()
        {
            var options = new DefaultRetryPolicyOptions
            {
                DeltaBackoff = TimeSpan.FromMilliseconds(100)
            };
            var retryPolicy = new DefaultRetryPolicy(options);
            var client = new FakeSender().Returns(HttpStatusCode.InternalServerError, HttpStatusCode.OK);

            var stopwatch = Stopwatch.StartNew();
            var response = await retryPolicy.ExecuteAsync(client.SendRequest);
            stopwatch.Stop();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, client.TimesCalled);
            Assert.True(stopwatch.Elapsed > 0.8 * options.DeltaBackoff);
        }

        [Fact]
        public async Task ExecuteAsync_RecoversAfterException()
        {
            var options = new DefaultRetryPolicyOptions
            {
                DeltaBackoff = TimeSpan.FromMilliseconds(100)
            };
            var retryPolicy = new DefaultRetryPolicy(options);
            var client = new FakeSender()
                .Throws(GetExceptionFromStatus(WebExceptionStatus.ConnectionClosed))
                .Returns(HttpStatusCode.OK);

            var stopwatch = Stopwatch.StartNew();
            var response = await retryPolicy.ExecuteAsync(client.SendRequest);
            stopwatch.Stop();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, client.TimesCalled);
            Assert.True(stopwatch.Elapsed > 0.8 * options.DeltaBackoff);
        }

        [Fact]
        public async Task ExecuteAsync_UnsuccessfulStatusCode_RetriesUntilCumulativeWaitTimeReached()
        {
            var options = new DefaultRetryPolicyOptions
            {
                DeltaBackoff = TimeSpan.FromMilliseconds(100),
                MaxCumulativeWaitTime = TimeSpan.FromSeconds(2)
            };
            var retryPolicy = new DefaultRetryPolicy(options);
            var client = new FakeSender().Returns(HttpStatusCode.InternalServerError);

            var stopwatch = Stopwatch.StartNew();
            var response = await retryPolicy.ExecuteAsync(client.SendRequest);
            stopwatch.Stop();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.True(client.TimesCalled > 1);
            var maximumPossibleNextWaitTime = 1.2 * options.DeltaBackoff * Math.Pow(2, client.TimesCalled - 1);
            Assert.True(stopwatch.Elapsed > options.MaxCumulativeWaitTime - maximumPossibleNextWaitTime);
        }

        [Fact]
        public async Task ExecuteAsync_Exception_RetriesUntilCumulativeWaitTimeReached()
        {
            var options = new DefaultRetryPolicyOptions
            {
                DeltaBackoff = TimeSpan.FromMilliseconds(100),
                MaxCumulativeWaitTime = TimeSpan.FromSeconds(2)
            };
            var retryPolicy = new DefaultRetryPolicy(options);
            var client = new FakeSender().Throws(GetExceptionFromStatus(WebExceptionStatus.ConnectionClosed));

            var stopwatch = Stopwatch.StartNew();
            await Assert.ThrowsAsync<HttpRequestException>(() => retryPolicy.ExecuteAsync(client.SendRequest));
            stopwatch.Stop();

            Assert.True(client.TimesCalled > 1);
            var maximumPossibleNextWaitTime = 1.2 * options.DeltaBackoff * Math.Pow(2, client.TimesCalled - 1);
            Assert.True(stopwatch.Elapsed > options.MaxCumulativeWaitTime - maximumPossibleNextWaitTime);
        }

        [Theory]
        [InlineData((HttpStatusCode)429)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        public async Task ExecuteAsync_ThrottledRequest_NoHeader_GetsNextWaitTime(HttpStatusCode statusCode)
        {
            var options = new DefaultRetryPolicyOptions
            {
                DeltaBackoff = TimeSpan.FromSeconds(10),
                MaxCumulativeWaitTime = TimeSpan.FromSeconds(5)
            };
            var retryPolicy = new DefaultRetryPolicy(options);
            var client = new FakeSender().Returns(statusCode);

            var response = await retryPolicy.ExecuteAsync(client.SendRequest);

            Assert.Equal(statusCode, response.StatusCode);
            Assert.Equal(1, client.TimesCalled);
        }

        [Theory]
        [InlineData((HttpStatusCode)429, -1)]
        [InlineData((HttpStatusCode)429, 0)]
        [InlineData(HttpStatusCode.ServiceUnavailable, -1)]
        [InlineData(HttpStatusCode.ServiceUnavailable, 0)]
        public async Task ExecuteAsync_ThrottledRequest_RetryAfterHeaderWithNotPositiveDelta_GetsNextWaitTime(HttpStatusCode statusCode, int waitTimeInSeconds)
        {
            var options = new DefaultRetryPolicyOptions
            {
                DeltaBackoff = TimeSpan.FromSeconds(10),
                MaxCumulativeWaitTime = TimeSpan.FromSeconds(5)
            };
            var retryPolicy = new DefaultRetryPolicy(options);
            var mockResponse = new HttpResponseMessage(statusCode);
            mockResponse.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(waitTimeInSeconds));
            var client = new FakeSender().Returns(mockResponse);

            var response = await retryPolicy.ExecuteAsync(client.SendRequest);

            Assert.Equal(statusCode, response.StatusCode);
            Assert.Equal(1, client.TimesCalled);
        }

        [Theory]
        [InlineData((HttpStatusCode)429)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        public async Task ExecuteAsync_ThrottledRequest_RetryAfterHeaderWithPastDate_GetsNextWaitTime(HttpStatusCode statusCode)
        {
            var options = new DefaultRetryPolicyOptions
            {
                DeltaBackoff = TimeSpan.FromSeconds(10),
                MaxCumulativeWaitTime = TimeSpan.FromSeconds(5)
            };
            var retryPolicy = new DefaultRetryPolicy(options);
            var mockResponse = new HttpResponseMessage(statusCode);
            mockResponse.Headers.RetryAfter = new RetryConditionHeaderValue(DateTime.UtcNow.AddSeconds(-1));
            var client = new FakeSender().Returns(mockResponse);

            var response = await retryPolicy.ExecuteAsync(client.SendRequest);

            Assert.Equal(statusCode, response.StatusCode);
            Assert.Equal(1, client.TimesCalled);
        }

        [Theory]
        [InlineData((HttpStatusCode)429)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        public async Task ExecuteAsync_ThrottledRequest_RetryAfterHeaderWithDelta_ReadsWaitTimeFromHeader(HttpStatusCode statusCode)
        {
            var options = new DefaultRetryPolicyOptions
            {
                DeltaBackoff = TimeSpan.FromMilliseconds(100),
                MaxCumulativeWaitTime = TimeSpan.FromSeconds(5)
            };
            var retryPolicy = new DefaultRetryPolicy(options);
            var mockResponse = new HttpResponseMessage(statusCode);
            mockResponse.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(6));
            var client = new FakeSender().Returns(mockResponse);

            var response = await retryPolicy.ExecuteAsync(client.SendRequest);

            Assert.Equal(statusCode, response.StatusCode);
            Assert.Equal(1, client.TimesCalled);
        }

        [Theory]
        [InlineData((HttpStatusCode)429)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        public async Task ExecuteAsync_ThrottledRequest_RetryAfterHeaderWithDate_ReadsWaitTimeFromHeader(HttpStatusCode statusCode)
        {
            var options = new DefaultRetryPolicyOptions
            {
                DeltaBackoff = TimeSpan.FromMilliseconds(100),
                MaxCumulativeWaitTime = TimeSpan.FromSeconds(5)
            };
            var retryPolicy = new DefaultRetryPolicy(options);
            var mockResponse = new HttpResponseMessage(statusCode);
            mockResponse.Headers.RetryAfter = new RetryConditionHeaderValue(DateTime.UtcNow.AddSeconds(6));
            var client = new FakeSender().Returns(mockResponse);

            var response = await retryPolicy.ExecuteAsync(client.SendRequest);

            Assert.Equal(statusCode, response.StatusCode);
            Assert.Equal(1, client.TimesCalled);
        }

        [Theory]
        [InlineData(HttpStatusCode.RequestTimeout)]
        [InlineData((HttpStatusCode)429)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        public async Task ExecuteAsync_RetriesForCertainStatusCodes(HttpStatusCode statusCode)
        {
            var options = new DefaultRetryPolicyOptions
            {
                DeltaBackoff = TimeSpan.FromMilliseconds(100)
            };
            var retryPolicy = new DefaultRetryPolicy(options);
            var client = new FakeSender().Returns(statusCode, HttpStatusCode.OK);

            var stopwatch = Stopwatch.StartNew();
            var response = await retryPolicy.ExecuteAsync(client.SendRequest);
            stopwatch.Stop();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, client.TimesCalled);
            Assert.True(stopwatch.Elapsed > 0.8 * options.DeltaBackoff);
        }

        [Theory]
        [MemberData(nameof(RetriedExceptions))]
        public async Task ExecuteAsync_RetriesForCertainExceptions(Exception exception)
        {
            var options = new DefaultRetryPolicyOptions
            {
                DeltaBackoff = TimeSpan.FromMilliseconds(100)
            };
            var retryPolicy = new DefaultRetryPolicy(options);
            var client = new FakeSender()
                .Throws(exception)
                .Returns(HttpStatusCode.OK);

            var stopwatch = Stopwatch.StartNew();
            var response = await retryPolicy.ExecuteAsync(client.SendRequest);
            stopwatch.Stop();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, client.TimesCalled);
            Assert.True(stopwatch.Elapsed > 0.8 * options.DeltaBackoff);
        }

        private static Exception GetExceptionFromStatus(WebExceptionStatus status) => new HttpRequestException("Exception", new WebException(string.Empty, status));
        public static readonly object[][] RetriedExceptions =
        {
            new object[] { GetExceptionFromStatus(WebExceptionStatus.ConnectFailure) },
            new object[] { GetExceptionFromStatus(WebExceptionStatus.ConnectionClosed) },
            new object[] { GetExceptionFromStatus(WebExceptionStatus.KeepAliveFailure) },
            new object[] { GetExceptionFromStatus(WebExceptionStatus.NameResolutionFailure) },
            new object[] { GetExceptionFromStatus(WebExceptionStatus.ReceiveFailure) },
            new object[] { GetExceptionFromStatus(WebExceptionStatus.SendFailure) },
            new object[] { GetExceptionFromStatus(WebExceptionStatus.Timeout) },
        };

        private class FakeSender
        {
            public int TimesCalled { get; private set; }
            private readonly Queue<Task<HttpResponseMessage>> _responses = new Queue<Task<HttpResponseMessage>>();

            public Task<HttpResponseMessage> SendRequest()
            {
                ++TimesCalled;

                if (_responses.Count == 1)
                {
                    return _responses.Peek();
                }
                return _responses.Any()
                    ? _responses.Dequeue()
                    : Task.FromResult((HttpResponseMessage)null);
            }

            public FakeSender Returns(params HttpResponseMessage[] responseMessages)
            {
                foreach (var response in responseMessages)
                {
                    _responses.Enqueue(Task.FromResult(response));
                }

                return this;
            }


            public FakeSender Returns(params HttpStatusCode[] statusCodes)
            {
                foreach (var statusCode in statusCodes)
                {
                    _responses.Enqueue(Task.FromResult(new HttpResponseMessage(statusCode)));
                }

                return this;
            }

            public FakeSender Throws(params Exception[] exceptions)
            {
                foreach (var exception in exceptions)
                {
                    _responses.Enqueue(Task.FromException<HttpResponseMessage>(exception));
                }

                return this;
            }
        }
    }
}