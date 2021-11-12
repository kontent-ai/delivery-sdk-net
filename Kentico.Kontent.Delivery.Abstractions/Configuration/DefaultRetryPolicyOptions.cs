using System;
using Kentico.Kontent.Delivery.Abstractions.RetryPolicy;

namespace Kentico.Kontent.Delivery.Abstractions.Configuration
{
    /// <summary>
    /// Represents configuration of the <see cref="IRetryPolicy"/> that performs retries using a randomized exponential back off scheme to determine the interval between retries.
    /// </summary>
    public class DefaultRetryPolicyOptions
    {
        /// <summary>
        /// Gets or sets the back-off interval between retries.
        /// The default value is 1 second.
        /// </summary>
        public TimeSpan DeltaBackoff { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum cumulative wait time.
        /// If the cumulative wait time exceeds this value, the client will stop retrying and return the error to the application.
        /// The default value is 30 seconds.
        /// </summary>
        public TimeSpan MaxCumulativeWaitTime { get; set; } = TimeSpan.FromSeconds(30);
    }
}