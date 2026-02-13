using Xunit;

namespace Kontent.Ai.Delivery.Abstractions.Tests.Caching;

public class CacheDependencyKeyBuilderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildItemDependencyKey_WithNullOrWhitespace_ReturnsNull(string? codename)
    {
        var key = CacheDependencyKeyBuilder.BuildItemDependencyKey(codename);

        Assert.Null(key);
    }

    [Fact]
    public void BuildItemDependencyKey_WithCodename_ReturnsPrefixedKey()
    {
        var key = CacheDependencyKeyBuilder.BuildItemDependencyKey("article");

        Assert.Equal("item_article", key);
    }

    [Fact]
    public void BuildAssetDependencyKey_ReturnsPrefixedKey()
    {
        var assetId = Guid.NewGuid();

        var key = CacheDependencyKeyBuilder.BuildAssetDependencyKey(assetId);

        Assert.Equal($"asset_{assetId}", key);
    }

    [Fact]
    public void BuildTaxonomyDependencyKey_WithCodename_ReturnsPrefixedKey()
    {
        var key = CacheDependencyKeyBuilder.BuildTaxonomyDependencyKey("categories");

        Assert.Equal("taxonomy_categories", key);
    }

    [Fact]
    public void BuildTypeDependencyKey_WithCodename_ReturnsPrefixedKey()
    {
        var key = CacheDependencyKeyBuilder.BuildTypeDependencyKey("article");

        Assert.Equal("type_article", key);
    }
}
