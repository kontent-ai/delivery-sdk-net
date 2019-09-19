using System;
using Kentico.Kontent.Delivery.RetryPolicy;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// The retry policy options that are used in the default implementation of the <see cref="IRetryPolicyProvider" /> interface.
    /// </summary>
    public class RetryPolicyOptions
    {
        /// <summary>
        /// Gets or sets the backoff interval associated with the retry.
        /// The default is 1 second.
        /// </summary>
        public TimeSpan DeltaBackoff { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the cumulative wait time associated with the retry.
        /// When cumulative wait time is reached the policy will return last response message or rethrow last handled exception.
        /// The default is 30 seconds.
        /// </summary>
        public TimeSpan MaxCumulativeWaitTime { get; set; } = TimeSpan.FromSeconds(30);
    }
}