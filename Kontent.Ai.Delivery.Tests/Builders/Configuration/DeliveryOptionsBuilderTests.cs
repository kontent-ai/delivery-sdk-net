using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Builders.Configuration
{
    public class DeliveryOptionsBuilderTests
    {
        private const string EnvironmentId = "550cec62-90a6-4ab3-b3e4-3d0bb4c04f5c";
        private const string PreviewApiKey = 
            "eyJ0eXAiOiwq14X65DLCJhbGciOiJIUzI1NiJ-.eyJqdGkiOiABCjJlM2FiOTBjOGM0ODVmYjdmZTDEFRQZGM1NDIyMCIsImlhdCI6IjE1Mjg454wexiLCJleHAiOiIxODc0NDg3NjqasdfwicHJvamVjdF9pZCI6Ij" +
            "g1OTEwOTlkN2458198ewqewZjI3Yzg5M2FhZTJiNTE4IiwidmVyIjoiMS4wLjAiLCJhdWQiewqgsdaWV3LmRlbGl2ZXIua2VudGljb2Nsb3VkLmNvbSJ9._tSzbNDpbE55dsaLUTGsdgesg4b693TFuhRCRsDyoc";

        private const string SecuredApiKey =
            "eyJ0eXAiOiwq14X65DLCJhbGciOiJIUzI1NiJ9.eyJqdGkiOiABCjJlM2FiOTBjOGM0ODVmYjdmZTDEFRQZGM1ND123QEwclhdCI6IjE1Mjg454wexiLCJleHAiOiIxODc0NDg3NjqasdfwicHJvamVjdF9pZCI6Ij" +
            "g1OTEwOTlkN2458198ewqewZjI3Yzg5M2FhZTJiNTE4IiwidmVyIjoiMS4wLjAiLCJhdWQiewqgsdaWV3LmRlbGl2ZXIua2VudGljb2Nsb3VkLmNvbSJ9.wtSzbNDpbE55dsaLUTGsdgesg4b693TFuhRCRsDyoc";
        private readonly Guid _guid = new Guid(EnvironmentId);

        [Fact]
        public void BuildWithEnvironmentIdAndUseProductionApi()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build();

            Assert.Equal(EnvironmentId, deliveryOptions.EnvironmentId);
            Assert.False(deliveryOptions.UsePreviewApi);
            Assert.False(deliveryOptions.UseSecureAccess);
        }

        [Fact]
        public void BuildWithEnvironmentIdAndPreviewApi()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(_guid)
                .UsePreviewApi(PreviewApiKey)
                .Build();

            Assert.Equal(EnvironmentId, deliveryOptions.EnvironmentId);
            Assert.True(deliveryOptions.UsePreviewApi);
            Assert.Equal(PreviewApiKey, deliveryOptions.PreviewApiKey);
        }

        [Fact]
        public void BuildWithEnvironmentIdAndSecuredProductionApi()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi(SecuredApiKey)
                .Build();

            Assert.Equal(EnvironmentId, deliveryOptions.EnvironmentId);
            Assert.True(deliveryOptions.UseSecureAccess);
            Assert.Equal(SecuredApiKey, deliveryOptions.SecureAccessApiKey);
        }
        
        [Fact]
        public void BuildWithRetryPolicyOptions()
        {
            var retryOptions = new DefaultRetryPolicyOptions();

            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .WithDefaultRetryPolicyOptions(retryOptions)
                .Build();

            Assert.Equal(retryOptions, deliveryOptions.DefaultRetryPolicyOptions);
        }

        [Fact]
        public void BuildWithNullRetryPolicyOptions_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .WithDefaultRetryPolicyOptions(null)
                .Build());
        }

        [Fact]
        public void BuildWithDisabledRetryLogic()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(Guid.NewGuid())
                .UseProductionApi()
                .DisableRetryPolicy()
                .Build();

            Assert.False(deliveryOptions.EnableRetryPolicy);
        }

        [Fact]
        public void BuildWithWaitForLoadingNewContent()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(Guid.NewGuid())
                .UseProductionApi()
                .WaitForLoadingNewContent()
                .Build();

            Assert.True(deliveryOptions.WaitForLoadingNewContent);
        }

        [Fact]
        public void BuildWithIncludeTotalCount()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(Guid.NewGuid())
                .UseProductionApi()
                .IncludeTotalCount()
                .Build();

            Assert.True(deliveryOptions.IncludeTotalCount);
        }

        [Fact]
        public void BuildWithCustomEndpointForPreviewApi()
        {
            const string customEndpoint = "http://www.customPreviewEndpoint.com";

            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(EnvironmentId)
                .UsePreviewApi(PreviewApiKey)
                .WithCustomEndpoint(customEndpoint)
                .Build();

           Assert.Equal(customEndpoint, deliveryOptions.PreviewEndpoint);
        }

        [Fact]
        public void BuildWithCustomEndpointForProductionApi()
        {
            const string customEndpoint = "https://www.customProductionEndpoint.com";

            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .WithCustomEndpoint(customEndpoint)
                .Build();

            Assert.Equal(customEndpoint, deliveryOptions.ProductionEndpoint);
        }

        [Fact]
        public void BuildWithCustomEndpointAsUriForPreviewApi()
        {
            const string customEndpoint = "http://www.custompreviewendpoint.com/";
            var uri = new Uri(customEndpoint, UriKind.Absolute);
            
            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(EnvironmentId)
                .UsePreviewApi(PreviewApiKey)
                .WithCustomEndpoint(uri)
                .Build();

            Assert.Equal(customEndpoint, deliveryOptions.PreviewEndpoint);
        }

        [Fact]
        public void BuildWithCustomEndpointAsUriForProductionApi()
        {
            const string customEndpoint = "https://www.customproductionendpoint.com/";
            var uri = new Uri(customEndpoint, UriKind.Absolute);

            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .WithCustomEndpoint(uri)
                .Build();

            Assert.Equal(customEndpoint, deliveryOptions.ProductionEndpoint);
        }
        
        [Fact]
        public void BuildWithDefaultRenditionPreset()
        {
            const string renditionPreset = "mobile";

            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .WithDefaultRenditionPreset(renditionPreset)
                .Build();

            Assert.Equal(renditionPreset, deliveryOptions.DefaultRenditionPreset);
        }
    }
}
