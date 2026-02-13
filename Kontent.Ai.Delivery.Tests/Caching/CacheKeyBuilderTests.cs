using Kontent.Ai.Delivery.Api.QueryParams.Items;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Caching;

public class CacheKeyBuilderTests
{
    [Fact]
    public void BuildItemKey_WithModelType_AppendsModelDiscriminator()
    {
        var parameters = new SingleItemParams();

        var key = CacheKeyBuilder.BuildItemKey("coffee_beverages_explained", parameters, typeof(Article));

        Assert.Contains("model=", key);
    }

    [Fact]
    public void BuildItemKey_WithDifferentModelTypes_ProducesDifferentKeys()
    {
        var parameters = new SingleItemParams();

        var articleKey = CacheKeyBuilder.BuildItemKey("coffee_beverages_explained", parameters, typeof(Article));
        var accessoryKey = CacheKeyBuilder.BuildItemKey("coffee_beverages_explained", parameters, typeof(Accessory));

        Assert.NotEqual(articleKey, accessoryKey);
    }

    [Fact]
    public void BuildItemKey_WithoutModelType_DoesNotAppendModelDiscriminator()
    {
        var parameters = new SingleItemParams();

        var key = CacheKeyBuilder.BuildItemKey("coffee_beverages_explained", parameters);

        Assert.DoesNotContain("model=", key);
    }
}
