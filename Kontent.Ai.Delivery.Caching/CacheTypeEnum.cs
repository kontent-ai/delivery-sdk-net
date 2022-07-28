namespace Kontent.Ai.Delivery.Caching
{
    /// <summary>
    /// Determines whether to use <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache" /> or <inheritdoc cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
    /// </summary>
    public enum CacheTypeEnum
    {
        /// <summary>
        /// Corresponds with <inheritdoc cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
        /// </summary>
        Memory,

        /// <summary>
        /// Corresponds with <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache" />
        /// </summary>
        Distributed
    }
}
