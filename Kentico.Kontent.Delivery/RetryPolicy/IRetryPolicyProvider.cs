namespace Kentico.Kontent.Delivery.RetryPolicy
{
    /// <summary>
    /// Provides a retry policy instance if enabled
    /// </summary>
    public interface IRetryPolicyProvider
    {
        /// <summary>
        ///  Gets the retry policy instance.
        /// </summary>
        /// <returns></returns>
        IRetryPolicy GetRetryPolicy();
    }
}