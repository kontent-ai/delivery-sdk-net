using Kontent.Ai.Delivery.Extensions;
using System.Diagnostics;
using Xunit;

namespace Kontent.Ai.Delivery.Tests
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

            Assert.Equal($"Kontent.Ai.Delivery.Tests;{sourceVersion}", value);
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
            Assert.Equal($"Kontent.Ai.Delivery.Tests;{sourceVersion}", source);
        }
    }
}
