using Kontent.Ai.Delivery.Api.Filtering;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class SerializedFilterCollectionTests
{
    [Fact]
    public void SerializedFilterCollection_PreservesDuplicateKeyEntries()
    {
        var filters = new SerializedFilterCollection
        {
            new KeyValuePair<string, string>("system.type[in]", "article"),
            new KeyValuePair<string, string>("system.type[in]", "article,blog_post")
        };

        Assert.Equal(2, filters.Count);
        Assert.Equal("system.type[in]", filters[0].Key);
        Assert.Equal("article", filters[0].Value);
        Assert.Equal("system.type[in]", filters[1].Key);
        Assert.Equal("article,blog_post", filters[1].Value);
    }

    [Fact]
    public void SerializedFilterCollection_ToQueryDictionary_GroupsValuesPerKey()
    {
        var filters = new SerializedFilterCollection
        {
            new KeyValuePair<string, string>("system.type[eq]", "article"),
            new KeyValuePair<string, string>("System.Type[eq]", "blog_post"),
            new KeyValuePair<string, string>("elements.category[contains]", "coffee")
        };

        var query = filters.ToQueryDictionary();

        Assert.NotNull(query);
        Assert.True(query.ContainsKey("system.type[eq]"));
        Assert.Equal(["article", "blog_post"], query["system.type[eq]"]);
        Assert.Equal(["coffee"], query["elements.category[contains]"]);
    }

    [Fact]
    public void FilterQueryParams_ToQueryDictionary_SerializedFilterCollection_DelegatesToInstance()
    {
        var filters = new SerializedFilterCollection
        {
            new KeyValuePair<string, string>("system.type[eq]", "article")
        };

        var result = FilterQueryParams.ToQueryDictionary(filters);

        Assert.NotNull(result);
        Assert.Equal(["article"], result["system.type[eq]"]);
    }

    [Fact]
    public void FilterQueryParams_ToQueryDictionary_EmptyCollection_ReturnsNull()
    {
        var filters = new SerializedFilterCollection();

        Assert.Null(FilterQueryParams.ToQueryDictionary(filters));
    }
}
