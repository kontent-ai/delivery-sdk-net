using System;
using Kentico.Kontent.Delivery.Builders.DeliveryOptions;
using Kentico.Kontent.Delivery.Configuration;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.Builders.DeliveryOptions
{
    public class DeliveryOptionsValidatorTests
    {
        private readonly Guid _guid = Guid.NewGuid();
        
        [Fact]
        public void ValidateRetryOptions_NegativeDeltaBackoff_Throws()
        {
            var deliveryOptions = new Configuration.DeliveryOptions
            {
                ProjectId = _guid.ToString(),
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
            var deliveryOptions = new Configuration.DeliveryOptions
            {
                ProjectId = _guid.ToString(),
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
            var deliveryOptions = new Configuration.DeliveryOptions
            {
                ProjectId = _guid.ToString(),
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
            var deliveryOptions = new Configuration.DeliveryOptions
            {
                ProjectId = _guid.ToString(),
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
            var deliveryOptions = new Configuration.DeliveryOptions
            {
                ProjectId = _guid.ToString(),
                DefaultRetryPolicyOptions = null
            };

            Assert.Throws<ArgumentNullException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateOptionsWithEmptyProjectId()
        {
            var deliveryOptions = new Configuration.DeliveryOptions {ProjectId = ""};

            Assert.Throws<ArgumentException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateOptionsWithNullProjectId()
        {
            var deliveryOptions = new Configuration.DeliveryOptions { ProjectId = null };

            Assert.Throws<ArgumentNullException>(() => deliveryOptions.Validate());
        }

        [Theory]
        [InlineData("123-456")]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        public void ValidateOptionsWithEmptyGuidProjectId(string projectId)
        {
            var deliveryOptions = new Configuration.DeliveryOptions { ProjectId = projectId };

            Assert.Throws<ArgumentException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateOptionsWithNullPreviewApiKey()
        {
            var deliveryOptionsStep = DeliveryOptionsBuilder
                .CreateInstance()
                .WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => deliveryOptionsStep.UsePreviewApi(null));
        }

        [Fact]
        public void ValidateOptionsWithNullSecuredApiKey()
        {
            var deliveryOptionsStep = DeliveryOptionsBuilder
                .CreateInstance()
                .WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => deliveryOptionsStep.UseProductionApi(null));
        }

        [Fact]
        public void ValidateOptionsBuiltWithBuilderWithIncorrectApiKeyFormat()
        {
            var deliveryOptions = DeliveryOptionsBuilder
                .CreateInstance()
                .WithProjectId(_guid);

            Assert.Throws<ArgumentException>(() => deliveryOptions.UsePreviewApi("badPreviewApiFormat"));
        }

        [Fact]
        public void ValidateOptionsUseOfPreviewAndProductionApiSimultaneously()
        {
            const string previewApiKey = "previewApiKey";
            const string productionApiKey = "productionApiKey";

            var deliveryOptions = new Configuration.DeliveryOptions
            {
                ProjectId = _guid.ToString(),
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
            var deliveryOptions = new Configuration.DeliveryOptions
            {
                ProjectId = _guid.ToString(),
                UsePreviewApi = true
            };

            Assert.Throws<InvalidOperationException>(() => deliveryOptions.Validate());
        }

        [Fact]
        public void ValidateOptionsWithEnabledSecuredApiWithSetKey()
        {
            var deliveryOptions = new Configuration.DeliveryOptions
            {
                ProjectId = _guid.ToString(),
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
                .WithProjectId(_guid)
                .UseProductionApi();

            Assert.Throws<ArgumentException>(() => deliveryOptionsSteps.WithCustomEndpoint(endpoint));
        }

        [Fact]
        public void ValidateOptionsWithNullUriEndpoint()
        {
            var deliveryOptionsSteps = DeliveryOptionsBuilder
                .CreateInstance()
                .WithProjectId(_guid)
                .UseProductionApi();

            Assert.Throws<ArgumentNullException>(() => deliveryOptionsSteps.WithCustomEndpoint((Uri)null));
        }

        [Fact]
        public void ValidateOptionsWithUriEndpointWrongScheme()
        {
            var incorrectSchemeUri = new Uri("ftp://www.abc.com");
            var deliveryOptionsSteps = DeliveryOptionsBuilder
                .CreateInstance()
                .WithProjectId(_guid)
                .UseProductionApi();

            Assert.Throws<ArgumentException>(() => deliveryOptionsSteps.WithCustomEndpoint(incorrectSchemeUri));
        }

        [Fact]
        public void ValidateOptionsWithRelativeUriEndpoint()
        {
            var relativeUri = new Uri("/abc/cde", UriKind.Relative);
            var deliveryOptionsSteps = DeliveryOptionsBuilder
                .CreateInstance()
                .WithProjectId(_guid)
                .UseProductionApi();

            Assert.Throws<ArgumentException>(() => deliveryOptionsSteps.WithCustomEndpoint(relativeUri));
        }

        [Fact]
        public void ValidateOptionsBuiltWithBuilderWithEmptyProjectId()
        {
            var deliveryOptionsSteps = DeliveryOptionsBuilder.CreateInstance();

            Assert.Throws<ArgumentException>(() => deliveryOptionsSteps.WithProjectId(Guid.Empty));
        }
    }
}
