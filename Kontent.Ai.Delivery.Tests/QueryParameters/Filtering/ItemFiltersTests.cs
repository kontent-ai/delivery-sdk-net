using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryParameters.Filtering;

public class ItemFiltersTests
{
    private readonly ItemFilters _filters = new();

    [Fact]
    public void Equals_BuildsCorrectFilter()
    {
        var filter = _filters.Equals(ItemSystemPath.Type, "article");

        Assert.Equal("system.type[eq]=\"article\"", filter.ToQueryParameter());
    }

    [Fact]
    public void All_BuildsCorrectFilter()
    {
        var filter = _filters.All(Elements.GetPath("tags"), "a", "b");

        Assert.Equal("elements.tags[all]=\"a\",\"b\"", filter.ToQueryParameter());
    }

    [Fact]
    public void Empty_BuildsCorrectFilter()
    {
        var filter = _filters.Empty(Elements.GetPath("title"));

        Assert.Equal("elements.title[empty]", filter.ToQueryParameter());
    }
}


