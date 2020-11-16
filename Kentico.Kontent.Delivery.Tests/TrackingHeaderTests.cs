using Kentico.Kontent.Delivery.Extensions;
using System.Diagnostics;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests
{
    public class TrackingHeaderTests
    {
        [Fact]
        public void SourceTrackingHeaderGeneratedFromAttribute()
        {
            var attr = new DeliverySourceTrackingHeaderAttribute("CustomModule", 1, 2, 3);

            var value = HttpRequestHeadersExtensions.GenerateSourceTrackingHeaderValue(GetType().Assembly, attr);

            Assert.Equal("CustomModule;1.2.3", value);
        }

        [Fact]
        public void SourceTrackingHeaderGeneratedFromAssembly()
        {
            var assembly = GetType().Assembly;
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            var sourceVersion = fileVersionInfo.ProductVersion;
            var attr = new DeliverySourceTrackingHeaderAttribute();

            var value = HttpRequestHeadersExtensions.GenerateSourceTrackingHeaderValue(assembly, attr);

            Assert.Equal($"Kentico.Kontent.Delivery.Tests;{sourceVersion}", value);
        }

        [Fact]
        public void SourceGeneratedCorrectly()
        {
            var sourceAssembly = HttpRequestHeadersExtensions.GetOriginatingAssembly();
            var sourceFileVersionInfo = FileVersionInfo.GetVersionInfo(sourceAssembly.Location);
            var sourceVersion = sourceFileVersionInfo.ProductVersion;

            // Act
            var source = HttpRequestHeadersExtensions.GetSource();

            // Assert
            Assert.Equal($"Kentico.Kontent.Delivery.Tests;{sourceVersion}", source);
        }
    }
}
