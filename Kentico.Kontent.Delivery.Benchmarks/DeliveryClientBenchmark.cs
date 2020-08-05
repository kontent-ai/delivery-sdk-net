using System;
using System.IO;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Kentico.Kontent.Delivery.Caching.Tests.ContentTypes;
using BenchmarkDotNet.Attributes;
using RichardSzalay.MockHttp;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.Benchmarks
{
    public class DeliveryClientBenchmark
    {
        private Guid _projectId;
        private string _baseUrl;
        private MockHttpMessageHandler _mockHttp;


        [GlobalSetup]
        public void Setup()
        {
            _projectId = Guid.NewGuid();
            var projectId = _projectId.ToString();
            _baseUrl = $"https://deliver.kontent.ai/{projectId}";
            _mockHttp = new MockHttpMessageHandler();
        }

        [Benchmark]
        public async Task<Abstractions.IDeliveryItemResponse<Coffee>> GetItemAsync()
        {
            // Arrange
            string url = $"{_baseUrl}/items/brazil_natural_barra_grande";

            _mockHttp
                .When(url)
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}brazil_natural_barra_grande.json")));

            var client = DeliveryClientBuilder.WithProjectId(_projectId).WithTypeProvider(new CustomTypeProvider()).WithDeliveryHttpClient(new DeliveryHttpClient(_mockHttp.ToHttpClient())).Build();

            // Act
            var response = await client.GetItemAsync<Coffee>("brazil_natural_barra_grande");

            return response;
        }

        [Benchmark]
        public async Task<Abstractions.IDeliveryItemListingResponse<Article>> GetItemsAsync()
        {
            // Arrange
            string url = $"{_baseUrl}/items";

            _mockHttp
                .When(url)
                .WithQueryString("system.type=article")
                .Respond("application/json",
                    await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}full_articles.json")));

            var client = DeliveryClientBuilder.WithProjectId(_projectId).WithTypeProvider(new CustomTypeProvider()).WithDeliveryHttpClient(new DeliveryHttpClient(_mockHttp.ToHttpClient())).Build();

            // Act
            var response = await client.GetItemsAsync<Article>();

            return response;
        }
    }
}
