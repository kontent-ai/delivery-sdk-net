using System;
using System.IO;
using Kontent.Ai.Delivery.Builders.DeliveryClient;
using Kontent.Ai.Delivery.Abstractions;
using BenchmarkDotNet.Attributes;
using RichardSzalay.MockHttp;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Benchmarks.ContentTypes;
using BenchmarkDotNet.Jobs;

namespace Kontent.Ai.Delivery.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net50)]
    public class DeliveryClientBenchmark
    {
        private IDeliveryClient _client;

        [GlobalSetup]
        public async Task Setup()
        {
            var environmentId = Guid.NewGuid();
            var baseUrl = $"https://deliver.kontent.ai/{environmentId}";
            var mockHttp = new MockHttpMessageHandler();

            mockHttp
                .When($"{baseUrl}/items/on_roasts")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}on_roasts.json")));

            mockHttp
                .When($"{baseUrl}/items")
                .WithQueryString("system.type=article")
                .Respond("application/json",
                    await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}full_articles.json")));

            _client = DeliveryClientBuilder.WithEnvironmentId(environmentId).WithTypeProvider(new CustomTypeProvider()).WithDeliveryHttpClient(new DeliveryHttpClient(mockHttp.ToHttpClient())).Build();
        }

        [Benchmark]
        public async Task<IDeliveryItemResponse<Article>> GetItemAsync()
        {
            return await _client.GetItemAsync<Article>("on_roasts");
        }

        [Benchmark]
        public async Task<IDeliveryItemListingResponse<Article>> GetItemsAsync()
        {
            return await _client.GetItemsAsync<Article>();
        }
    }
}
