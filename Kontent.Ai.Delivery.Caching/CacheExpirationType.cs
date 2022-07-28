namespace Kontent.Ai.Delivery.Caching
{
    /// <summary>
    /// Determines which expiration type to use
    /// </summary>
    public enum CacheExpirationType
    {
        /// <summary>
        /// Sliding expiration type
        /// </summary>
        Sliding = 0,

        /// <summary>
        /// Absolute expiration type
        /// </summary>
        Absolute = 1
    }
}
