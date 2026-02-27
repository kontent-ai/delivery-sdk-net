using Kontent.Ai.Delivery.Api.QueryParams.Items;
using Kontent.Ai.Delivery.Api.QueryParams.TaxonomyGroups;
using Kontent.Ai.Delivery.Api.QueryParams.Types;
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

    [Fact]
    public void BuildItemKey_WithLanguage_AppendsLanguage()
    {
        var parameters = new SingleItemParams { Language = "en-US" };

        var key = CacheKeyBuilder.BuildItemKey("hero", parameters);

        Assert.Contains("lang=en-US", key);
    }

    [Fact]
    public void BuildItemKey_WithDepth_AppendsDepth()
    {
        var parameters = new SingleItemParams { Depth = 3 };

        var key = CacheKeyBuilder.BuildItemKey("hero", parameters);

        Assert.Contains("depth=3", key);
    }

    [Fact]
    public void BuildItemKey_WithExcludeElements_AppendsExcludeProjection()
    {
        var parameters = new SingleItemParams { ExcludeElements = "body,teaser" };

        var key = CacheKeyBuilder.BuildItemKey("hero", parameters);

        Assert.Contains("exclude=", key);
        Assert.DoesNotContain("elements=", key);
    }

    [Fact]
    public void BuildItemsKey_WithOrderBy_AppendsOrder()
    {
        var parameters = new ListItemsParams { OrderBy = "system.name[asc]" };

        var key = CacheKeyBuilder.BuildItemsKey(parameters, []);

        Assert.Contains("order=system.name[asc]", key);
    }

    [Fact]
    public void BuildItemsKey_WithIncludeTotalCount_AppendsTotal()
    {
        var parameters = new ListItemsParams { IncludeTotalCount = true };

        var key = CacheKeyBuilder.BuildItemsKey(parameters, []);

        Assert.Contains("total", key);
    }

    [Fact]
    public void BuildItemsKey_WithFilters_AppendsFilterHash()
    {
        var filters = new List<KeyValuePair<string, string>>
        {
            new("system.type[eq]", "article")
        };
        var parameters = new ListItemsParams();

        var key = CacheKeyBuilder.BuildItemsKey(parameters, filters);

        Assert.Contains("filters=", key);
    }

    [Fact]
    public void BuildItemsKey_WithFilters_IsDeterministic()
    {
        var filters = new List<KeyValuePair<string, string>>
        {
            new("system.type[eq]", "article"),
            new("elements.category[contains]", "coffee")
        };
        var parameters = new ListItemsParams();

        var key1 = CacheKeyBuilder.BuildItemsKey(parameters, filters);
        var key2 = CacheKeyBuilder.BuildItemsKey(parameters, filters);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void BuildItemsKey_WithAllParams_ProducesExpectedFormat()
    {
        var parameters = new ListItemsParams
        {
            Language = "en-US",
            Depth = 2,
            Skip = 0,
            Limit = 10,
            OrderBy = "system.name[asc]",
            IncludeTotalCount = true,
            Elements = "title,description"
        };
        var filters = new List<KeyValuePair<string, string>>
        {
            new("system.type[eq]", "article")
        };

        var key = CacheKeyBuilder.BuildItemsKey(parameters, filters);

        Assert.StartsWith("items:", key);
        Assert.Contains("lang=en-US", key);
        Assert.Contains("depth=2", key);
        Assert.Contains("skip=0", key);
        Assert.Contains("limit=10", key);
        Assert.Contains("order=system.name[asc]", key);
        Assert.Contains("total", key);
        Assert.Contains("elements=", key);
        Assert.Contains("filters=", key);
    }

    [Fact]
    public void BuildTaxonomiesKey_WithFilters_AppendsFilterHash()
    {
        var parameters = new ListTaxonomyGroupsParams { Skip = 0, Limit = 100 };
        var filters = new List<KeyValuePair<string, string>>
        {
            new("system.codename[eq]", "personas")
        };

        var key = CacheKeyBuilder.BuildTaxonomiesKey(parameters, filters);

        Assert.StartsWith("taxonomies:", key);
        Assert.Contains("filters=", key);
    }

    [Fact]
    public void BuildTypesKey_WithElements_AppendsElementProjection()
    {
        var parameters = new ListTypesParams { Elements = "codename,name" };

        var key = CacheKeyBuilder.BuildTypesKey(parameters, []);

        Assert.StartsWith("types:", key);
        Assert.Contains("elements=", key);
    }

    [Fact]
    public void BuildTaxonomyKey_ProducesExpectedFormat()
    {
        var key = CacheKeyBuilder.BuildTaxonomyKey("personas");

        Assert.Equal("taxonomy:personas", key);
    }

    [Fact]
    public void BuildTypeKey_WithElements_ProducesExpectedFormat()
    {
        var parameters = new SingleTypeParams { Elements = "codename" };

        var key = CacheKeyBuilder.BuildTypeKey("article", parameters);

        Assert.StartsWith("type:article:", key);
        Assert.Contains("elements=codename", key);
    }
}
