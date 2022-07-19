using System;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.RetryPolicy
{
    internal class DefaultRetryPolicyProvider : IRetryPolicyProvider
    {
        private readonly DefaultRetryPolicyOptions _retryPolicyOptions;

        public DefaultRetryPolicyProvider(IOptions<DeliveryOptions> options)
        {
            _retryPolicyOptions = options.Value.DefaultRetryPolicyOptions ?? throw new ArgumentNullException(nameof(options));
        }

        public IRetryPolicy GetRetryPolicy() => new DefaultRetryPolicy(_retryPolicyOptions);
    }
}