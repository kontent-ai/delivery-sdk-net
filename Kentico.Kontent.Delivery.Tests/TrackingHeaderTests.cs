using FakeItEasy;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Extensions;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests
{
    [Collection("Tracking header tests")]
    public class TrackingHeaderTests
    {
        private readonly Guid _guid;
        private readonly string _baseUrl;
        private readonly MockHttpMessageHandler _mockHttp;

        public TrackingHeaderTests()
        {
            _guid = Guid.NewGuid();
            _baseUrl = $"https://deliver.kontent.ai/{_guid.ToString()}";
            _mockHttp = new MockHttpMessageHandler();
        }

        [Fact]
        public void SourceTrackingHeaderGeneratedFromAttribute()
        {
            DeliverySourceTrackingHeaderAttribute attr = new DeliverySourceTrackingHeaderAttribute("CustomModule", 1, 2, 3);

            var value = HttpRequestHeadersExtensions.GenerateSourceTrackingHeaderValue(GetType().Assembly, attr);

            Assert.Equal("CustomModule;1.2.3", value);
        }

        [Fact]
        public void SourceTrackingHeaderGeneratedFromAssembly()
        {
            var assembly = this.GetType().Assembly;
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            var sourceVersion = fileVersionInfo.ProductVersion;
            DeliverySourceTrackingHeaderAttribute attr = new DeliverySourceTrackingHeaderAttribute();

            var value = HttpRequestHeadersExtensions.GenerateSourceTrackingHeaderValue(GetType().Assembly, attr);

            Assert.Equal($"Kentico.Kontent.Delivery.Tests;{sourceVersion}", value);
        }

        [Fact]
        public async Task CorrectSdkVersionHeaderAdded()
        {
            var assembly = typeof(DeliveryClient).Assembly;
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            var sdkVersion = fileVersionInfo.ProductVersion;
            var sdkPackageId = assembly.GetName().Name;

            var sourceAssembly = this.GetType().Assembly;
            var sourceFileVersionInfo = FileVersionInfo.GetVersionInfo(sourceAssembly.Location);
            var sourceVersion = sourceFileVersionInfo.ProductVersion;

            _mockHttp
                .Expect($"{_baseUrl}/items")
                .WithHeaders("X-KC-SDKID", $"nuget.org;{sdkPackageId};{sdkVersion}").WithHeaders("X-KC-SOURCE", $"Kentico.Kontent.Delivery.Tests;{sourceVersion}")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(_guid, _mockHttp);

            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy())
                .Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            await client.GetItemsAsync<object>();

            _mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
