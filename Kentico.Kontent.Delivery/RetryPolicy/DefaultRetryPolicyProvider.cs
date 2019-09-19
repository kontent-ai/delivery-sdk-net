using System;
using Microsoft.Extensions.Options;

namespace Kentico.Kontent.Delivery.RetryPolicy
{
    internal class DefaultRetryPolicyProvider : IRetryPolicyProvider
    {
        public DefaultRetryPolicyProvider(IOptions<DeliveryOptions> options)
        {
        }

        public IRetryPolicy GetRetryPolicy() => throw new NotImplementedException();
    }
}