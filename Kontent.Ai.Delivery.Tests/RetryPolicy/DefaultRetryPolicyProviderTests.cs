using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.RetryPolicy;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.RetryPolicy
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