namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Provides a retry policy for <see cref="IDeliveryClient"/>.
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