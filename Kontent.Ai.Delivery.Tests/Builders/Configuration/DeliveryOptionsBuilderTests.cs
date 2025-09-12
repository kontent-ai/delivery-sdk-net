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
        [Obsolete]
        public void BuildWithEnvironmentIdAndUseProductionApi()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .Create(EnvironmentId)
                .UseProduction()
                .Build();

            Assert.Equal(EnvironmentId, deliveryOptions.EnvironmentId);
            Assert.False(deliveryOptions.UsePreviewApi);
            Assert.False(deliveryOptions.UseSecureAccess);
        }

        [Fact]
        [Obsolete]
        public void BuildWithEnvironmentIdAndPreviewApi()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .Create(EnvironmentId)
                .UsePreview(PreviewApiKey)
                .Build();

            Assert.Equal(EnvironmentId, deliveryOptions.EnvironmentId);
            Assert.True(deliveryOptions.UsePreviewApi);
            Assert.Equal(PreviewApiKey, deliveryOptions.PreviewApiKey);
        }

        [Fact]
        [Obsolete]
        public void BuildWithEnvironmentIdAndSecuredProductionApi()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .Create(EnvironmentId)
                .UseProduction(SecuredApiKey)
                .Build();

            Assert.Equal(EnvironmentId, deliveryOptions.EnvironmentId);
            Assert.True(deliveryOptions.UseSecureAccess);
            Assert.Equal(SecuredApiKey, deliveryOptions.SecureAccessApiKey);
        }

        // [Fact]
        // [Obsolete]
        // public void BuildWithRetryPolicyOptions()
        // {
        //     var retryOptions = new DefaultRetryPolicyOptions();

        //     var deliveryOptions = DeliveryOptionsBuilder
        //         .Create(EnvironmentId)
        //         .UseProduction()
        //         .Build();

        //     Assert.Equal(retryOptions, deliveryOptions.DefaultRetryPolicyOptions);
        // }

        // [Fact]
        // [Obsolete]
        // public void BuildWithNullRetryPolicyOptions_ThrowsException()
        // {
        //     Assert.Throws<ArgumentNullException>(() => DeliveryOptionsBuilder
        //         .CreateInstance()
        //         .WithEnvironmentId(EnvironmentId)
        //         .UseProductionApi()
        //         .WithDefaultRetryPolicyOptions(null)
        //         .Build());
        // }

        [Fact]
        public void BuildWithDisabledRetryLogic()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .Create(Guid.NewGuid())
                .UseProduction()
                .DisableRetryPolicy()
                .Build();

            Assert.False(deliveryOptions.EnableResilience);
        }

        [Fact]
        public void ModernBuilder_WithWaitForLoadingNewContent_SetsOptions()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .Create(EnvironmentId)
                .UseProduction()
                .WaitForLoadingNewContent()
                .Build();

            Assert.True(deliveryOptions.WaitForLoadingNewContent);
        }

        // [Fact]
        // [Obsolete]
        // public void BuildWithIncludeTotalCount()
        // {
        //     var deliveryOptions = DeliveryOptionsBuilder
        //         .Create(Guid.NewGuid())
        //         .UseProduction()
        //         .IncludeTotalCount()
        //         .Build();

        //     Assert.True(deliveryOptions.IncludeTotalCount);
        // }

        [Fact]
        public void BuildWithCustomEndpointForPreviewApi()
        {
            const string customEndpoint = "http://www.customPreviewEndpoint.com";

            var deliveryOptions = DeliveryOptionsBuilder
                .Create(EnvironmentId)
                .UsePreview(PreviewApiKey)
                .WithCustomEndpoint(customEndpoint)
                .Build();

            Assert.Equal(customEndpoint, deliveryOptions.PreviewEndpoint);
        }

        [Fact]
        public void BuildWithCustomEndpointForProductionApi()
        {
            const string customEndpoint = "https://www.customProductionEndpoint.com";

            var deliveryOptions = DeliveryOptionsBuilder
                .Create(EnvironmentId)
                .UseProduction()
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
                .Create(EnvironmentId)
                .UsePreview(PreviewApiKey)
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
                .Create(EnvironmentId)
                .UseProduction()
                .WithCustomEndpoint(uri)
                .Build();

            Assert.Equal(customEndpoint, deliveryOptions.ProductionEndpoint);
        }

        [Fact]
        public void BuildWithDefaultRenditionPreset()
        {
            const string renditionPreset = "mobile";

            var deliveryOptions = DeliveryOptionsBuilder
                .Create(EnvironmentId)
                .UseProduction()
                .WithDefaultRenditionPreset(renditionPreset)
                .Build();

            Assert.Equal(renditionPreset, deliveryOptions.DefaultRenditionPreset);
        }
    }
}
