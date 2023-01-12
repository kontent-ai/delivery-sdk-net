namespace Kontent.Ai.Delivery.Caching
{
    /// <summary>
    /// Determines which resilient policy should be applied when Distrubuted cache is not available
    /// </summary>
    public enum DistributedCacheResilientPolicy
    {
        /// <summary>
        /// No resilient policy is used, throw exception
        /// </summary>
        Crash = 0,

        /// <summary>
        /// Fallback to default DeliveryClient implementation
        /// </summary>
        FallbackToApi = 1
    }
}
