namespace Kentico.Kontent.Delivery.Caching.Extensions
{
    internal static class DeliveryCacheOptionsExtensions
    {
        public static void Configure(this DeliveryCacheOptions o, DeliveryCacheOptions options)
        {
            o.CacheType = options.CacheType;
            o.DefaultExpiration = options.DefaultExpiration;
            o.DefaultExpirationType = options.DefaultExpirationType;
            o.StaleContentExpiration = options.StaleContentExpiration;
            o.Name = o.Name;
        }
    }
}
