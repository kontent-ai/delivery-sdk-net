namespace Kontent.Ai.Delivery.Caching.Extensions
{
    /// <summary>
    /// Extensions for a <see cref="DeliveryCacheOptions"/>.
    /// </summary>
    public static class DeliveryCacheOptionsExtensions
    {
        /// <summary>
        /// Maps one <see cref="DeliveryCacheOptions"/> object to another.
        /// </summary>
        /// <param name="o">A destination.</param>
        /// <param name="options">A source.</param>
        public static void Configure(this DeliveryCacheOptions o, DeliveryCacheOptions options)
        {
            o.CacheType = options.CacheType;
            o.DefaultExpiration = options.DefaultExpiration;
            o.DefaultExpirationType = options.DefaultExpirationType;
            o.StaleContentExpiration = options.StaleContentExpiration;
            o.Name = options.Name;
        }
    }
}
