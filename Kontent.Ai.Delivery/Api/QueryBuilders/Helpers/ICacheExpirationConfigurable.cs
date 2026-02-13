namespace Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;

internal interface ICacheExpirationConfigurable
{
    TimeSpan? CacheExpiration { get; set; }
}
