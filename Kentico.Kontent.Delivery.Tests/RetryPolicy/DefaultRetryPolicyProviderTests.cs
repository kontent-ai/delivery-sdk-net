using System;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.Configuration;
using Kentico.Kontent.Delivery.RetryPolicy;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.RetryPolicy
{
    public class DefaultRetryPolicyProviderTests
    {
        [Fact]
        public void Constructor_NullRetryPolicyOptions_ThrowsArgumentNullException()
        {
            var deliveryOptions = Options.Create(new DeliveryOptions
            {
                DefaultRetryPolicyOptions = null
            });

            Assert.Throws<ArgumentNullException>(() => new DefaultRetryPolicyProvider(deliveryOptions));
        }

        [Fact]
        public void GetRetryPolicy_ReturnsDefaultPolicy()
        {
            var deliveryOptions = Options.Create(new DeliveryOptions
            {
                DefaultRetryPolicyOptions = new DefaultRetryPolicyOptions()
            });
            var provider = new DefaultRetryPolicyProvider(deliveryOptions);

            var retryPolicy = provider.GetRetryPolicy();

            Assert.NotNull(retryPolicy);
            Assert.IsType<DefaultRetryPolicy>(retryPolicy);

        }
    }
}