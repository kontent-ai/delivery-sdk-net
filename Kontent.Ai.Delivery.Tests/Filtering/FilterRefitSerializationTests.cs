using System.Net;
using System.Text;
using Kontent.Ai.Delivery.Api;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.Configuration;
using Refit;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class FilterRefitSerializationTests
{
    [Fact]
    public async Task Refit_Encodes_RawFilterValue_Once()
    {
        var handler = new CaptureHandler();
        var api = CreateApi(handler);

        var filters = new Dictionary<string, string[]>
        {
            ["elements.title[eq]"] = ["Hello & World"]
        };

        await api.GetItemsInternalAsync<object>(queryParameters: null, filters: filters);

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("Hello%20%26%20World", query);
        Assert.DoesNotContain("Hello%2520%2526%2520World", query);
    }

    [Fact]
    public async Task Refit_DoubleEncodes_PreEncodedFilterValue()
    {
        var handler = new CaptureHandler();
        var api = CreateApi(handler);

        // Guardrail: if a caller pre-encodes, Refit will encode again.
        var preEncoded = "Hello%20%26%20World";
        var filters = new Dictionary<string, string[]>
        {
            ["elements.title[eq]"] = [preEncoded]
        };

        await api.GetItemsInternalAsync<object>(queryParameters: null, filters: filters);

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("Hello%2520%2526%2520World", query);
    }

    [Fact]
    public async Task FilterDsl_EncodedValue_IsEncodedAgain_ByRefit()
    {
        var handler = new CaptureHandler();
        var api = CreateApi(handler);

        var serialized = new List<KeyValuePair<string, string>>();
        var builder = new ItemsFilterBuilder(serialized);
        builder.Element("title").IsEqualTo("Hello & World"); // DSL keeps raw; Refit encodes once

        var filters = FilterQueryParams.ToQueryDictionary(serialized)!;
        await api.GetItemsInternalAsync<object>(queryParameters: null, filters: filters);

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("Hello%20%26%20World", query);
        Assert.DoesNotContain("Hello%2520%2526%2520World", query);
    }

    [Fact]
    public async Task FilterDsl_EmptyOperator_IsSerializedAsKeySuffix()
    {
        var handler = new CaptureHandler();
        var api = CreateApi(handler);

        var serialized = new List<KeyValuePair<string, string>>();
        var builder = new ItemsFilterBuilder(serialized);
        builder.Element("title").IsEmpty();

        var filters = FilterQueryParams.ToQueryDictionary(serialized)!;
        await api.GetItemsInternalAsync<object>(queryParameters: null, filters: filters);

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("elements.title%5Bempty%5D", query);
    }

    private static IDeliveryApi CreateApi(CaptureHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test")
        };

        var settings = RefitSettingsProvider.CreateDefaultSettings();
        return RestService.For<IDeliveryApi>(httpClient, settings);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            // Return a minimal successful payload. Items are empty, so no model conversion is needed.
            const string json = """
                                {
                                  "items": [],
                                  "pagination": { "skip": 0, "limit": 1, "count": 0, "next_page": "" },
                                  "modular_content": {}
                                }
                                """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}

