using System;
using Microsoft.Extensions.Options;

namespace Kentico.Kontent.Delivery.RetryPolicy
{
    internal class DefaultRetryPolicyProvider : IRetryPolicyProvider
    {
        private readonly RetryPolicyOptions _retryPolicyOptions;

        public DefaultRetryPolicyProvider(IOptions<DeliveryOptions> options)
        {
            _retryPolicyOptions = options.Value.RetryPolicyOptions ?? throw new ArgumentNullException(nameof(options));
        }

        public IRetryPolicy GetRetryPolicy() => new DefaultRetryPolicy(_retryPolicyOptions);
    }
}