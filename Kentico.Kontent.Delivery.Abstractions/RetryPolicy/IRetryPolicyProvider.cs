namespace Kentico.Kontent.Delivery.Abstractions.RetryPolicy
{
    /// <summary>
    /// Provides a retry policy for <see cref="DeliveryClient"/>.
    /// </summary>
    public interface IRetryPolicyProvider
    {
        /// <summary>
        /// Returns a new instance of a retry policy.
        /// </summary>
        /// <returns>A new instance of a retry policy.</returns>
        IRetryPolicy GetRetryPolicy();
    }
}