﻿using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Builders.Configuration
{
    public class DeliveryOptionsValidatorTests
    {
        private readonly Guid _guid = Guid.NewGuid();
        
        [Fact]
        public void ValidateRetryOptions_NegativeDeltaBackoff_Throws()
        {
            var deliveryOptions = new DeliveryOptions
            {
                EnvironmentId = _guid.ToString(),
                DefaultRetryPolicyOptions = new DefaultRetryPolicyOptions
                {
                    DeltaBackoff = TimeSpan.FromSeconds(-1)
                }
            };

            Assert.Throws<ArgumentException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateRetryOptions_ZeroDeltaBackoff_Throws()
        {
            var deliveryOptions = new DeliveryOptions
            {
                EnvironmentId = _guid.ToString(),
                DefaultRetryPolicyOptions = new DefaultRetryPolicyOptions
                {
                    DeltaBackoff = TimeSpan.Zero
                }
            };

            Assert.Throws<ArgumentException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateRetryOptions_NegativeMaxCumulativeWaitTime_Throws()
        {
            var deliveryOptions = new DeliveryOptions
            {
                EnvironmentId = _guid.ToString(),
                DefaultRetryPolicyOptions = new DefaultRetryPolicyOptions
                {
                    MaxCumulativeWaitTime = TimeSpan.FromSeconds(-1)
                }
            };

            Assert.Throws<ArgumentException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateRetryOptions_ZeroMaxCumulativeWaitTime_Throws()
        {
            var deliveryOptions = new DeliveryOptions
            {
                EnvironmentId = _guid.ToString(),
                DefaultRetryPolicyOptions = new DefaultRetryPolicyOptions
                {
                    MaxCumulativeWaitTime = TimeSpan.Zero
                }
            };

            Assert.Throws<ArgumentException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateNullRetryOptions_Throws()
        {
            var deliveryOptions = new DeliveryOptions
            {
                EnvironmentId = _guid.ToString(),
                DefaultRetryPolicyOptions = null
            };

            Assert.Throws<ArgumentNullException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateOptionsWithEmptyEnvironmentId()
        {
            var deliveryOptions = new DeliveryOptions {EnvironmentId = ""};

            Assert.Throws<ArgumentException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateOptionsWithNullEnvironmentId()
        {
            var deliveryOptions = new DeliveryOptions { EnvironmentId = null };

            Assert.Throws<ArgumentNullException>(() => deliveryOptions.Validate());
        }

        [Theory]
        [InlineData("123-456")]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        public void ValidateOptionsWithEmptyGuidEnvironmentId(string environmentId)
        {
            var deliveryOptions = new DeliveryOptions { EnvironmentId = environmentId };

            Assert.Throws<ArgumentException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateOptionsWithNullPreviewApiKey()
        {
            var deliveryOptionsStep = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(_guid);

            Assert.Throws<ArgumentNullException>(() => deliveryOptionsStep.UsePreviewApi(null));
        }

        [Fact]
        public void ValidateOptionsWithNullSecuredApiKey()
        {
            var deliveryOptionsStep = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(_guid);

            Assert.Throws<ArgumentNullException>(() => deliveryOptionsStep.UseProductionApi(null));
        }

        [Fact]
        public void ValidateOptionsBuiltWithBuilderWithIncorrectApiKeyFormat()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(_guid);

            Assert.Throws<ArgumentException>(() => deliveryOptions.UsePreviewApi("badPreviewApiFormat"));
        }

        [Fact]
        public void ValidateOptionsUseOfPreviewAndProductionApiSimultaneously()
        {
            const string previewApiKey = "previewApiKey";
            const string productionApiKey = "productionApiKey";

            var deliveryOptions = new DeliveryOptions
            {
                EnvironmentId = _guid.ToString(),
                UsePreviewApi = true,
                PreviewApiKey = previewApiKey,
                UseSecureAccess = true,
                SecureAccessApiKey = productionApiKey
            };

            Assert.Throws<InvalidOperationException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateOptionsWithEnabledPreviewApiWithSetKey()
        {
            var deliveryOptions = new DeliveryOptions
            {
                EnvironmentId = _guid.ToString(),
                UsePreviewApi = true
            };

            Assert.Throws<InvalidOperationException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateOptionsWithEnabledSecuredApiWithSetKey()
        {
            var deliveryOptions = new DeliveryOptions
            {
                EnvironmentId = _guid.ToString(),
                UseSecureAccess = true
            };

            Assert.Throws<InvalidOperationException>(() => deliveryOptions.Validate());
        }

        [Theory]
        [InlineData("")]
        [InlineData("ftp://abc.com")]
        [InlineData("abc.com/{0}")]
        public void ValidateOptionsWithInvalidEndpointFormat(string endpoint)
        {
            var deliveryOptionsSteps = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(_guid)
                .UseProductionApi();

            Assert.Throws<ArgumentException>(() => deliveryOptionsSteps.WithCustomEndpoint(endpoint));
        }

        [Fact]
        public void ValidateOptionsWithNullUriEndpoint()
        {
            var deliveryOptionsSteps = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(_guid)
                .UseProductionApi();

            Assert.Throws<ArgumentNullException>(() => deliveryOptionsSteps.WithCustomEndpoint((Uri)null));
        }

        [Fact]
        public void ValidateOptionsWithUriEndpointWrongScheme()
        {
            var incorrectSchemeUri = new Uri("ftp://www.abc.com");
            var deliveryOptionsSteps = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(_guid)
                .UseProductionApi();

            Assert.Throws<ArgumentException>(() => deliveryOptionsSteps.WithCustomEndpoint(incorrectSchemeUri));
        }

        [Fact]
        public void ValidateOptionsWithRelativeUriEndpoint()
        {
            var relativeUri = new Uri("/abc/cde", UriKind.Relative);
            var deliveryOptionsSteps = DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(_guid)
                .UseProductionApi();

            Assert.Throws<ArgumentException>(() => deliveryOptionsSteps.WithCustomEndpoint(relativeUri));
        }

        [Fact]
        public void ValidateOptionsBuiltWithBuilderWithEmptyEnvironmentId()
        {
            var deliveryOptionsSteps = DeliveryOptionsBuilder.CreateInstance();

            Assert.Throws<ArgumentException>(() => deliveryOptionsSteps.WithEnvironmentId(Guid.Empty));
        }
    }
}
