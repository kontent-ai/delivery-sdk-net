using Kontent.Ai.Delivery.Api.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class FilterTests
{
    #region Equality Operators

    [Fact]
    public void IsEqualTo_String_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("title").IsEqualTo("HelloWorld");

        Assert.Contains(new KeyValuePair<string, string>("elements.title[eq]", "HelloWorld"), filters);
    }

    [Fact]
    public void IsEqualTo_Number_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("price").IsEqualTo(19.99);

        Assert.Contains(new KeyValuePair<string, string>("elements.price[eq]", "19.99"), filters);
    }

    [Fact]
    public void IsNotEqualTo_String_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("status").IsNotEqualTo("draft");

        Assert.Contains(new KeyValuePair<string, string>("elements.status[neq]", "draft"), filters);
    }

    [Fact]
    public void IsNotEqualTo_Number_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("quantity").IsNotEqualTo(0.0);

        Assert.Contains(new KeyValuePair<string, string>("elements.quantity[neq]", "0"), filters);
    }

    #endregion

    #region Comparison Operators

    [Fact]
    public void IsLessThan_Number_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("price").IsLessThan(100.0);

        Assert.Contains(new KeyValuePair<string, string>("elements.price[lt]", "100"), filters);
    }

    [Fact]
    public void IsLessThanOrEqualTo_Number_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("quantity").IsLessThanOrEqualTo(50.0);

        Assert.Contains(new KeyValuePair<string, string>("elements.quantity[lte]", "50"), filters);
    }

    [Fact]
    public void IsGreaterThan_Number_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("rating").IsGreaterThan(4.0);

        Assert.Contains(new KeyValuePair<string, string>("elements.rating[gt]", "4"), filters);
    }

    [Fact]
    public void IsGreaterThanOrEqualTo_Number_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("stock").IsGreaterThanOrEqualTo(10.0);

        Assert.Contains(new KeyValuePair<string, string>("elements.stock[gte]", "10"), filters);
    }

    [Fact]
    public void IsLessThan_DateTime_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("publish_date").IsLessThan(DateTime.Parse("2024-01-01"));

        Assert.Contains(new KeyValuePair<string, string>("elements.publish_date[lt]", "2024-01-01T00:00:00Z"), filters);
    }

    [Fact]
    public void IsGreaterThan_DateTime_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("created_at").IsGreaterThan(DateTime.Parse("2023-06-15"));

        Assert.Contains(new KeyValuePair<string, string>("elements.created_at[gt]", "2023-06-15T00:00:00Z"), filters);
    }

    #endregion

    #region Array Operators

    [Fact]
    public void In_UsesCommaSeparatedValues()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("tags").IsIn("a", "b");

        Assert.Contains(new KeyValuePair<string, string>("elements.tags[in]", "a,b"), filters);
    }

    [Fact]
    public void In_Numbers_UsesCommaSeparatedValues()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("sizes").IsIn(10.0, 20.0, 30.0);

        Assert.Contains(new KeyValuePair<string, string>("elements.sizes[in]", "10,20,30"), filters);
    }

    [Fact]
    public void Contains_SingleValue_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("categories").Contains("electronics");

        Assert.Contains(new KeyValuePair<string, string>("elements.categories[contains]", "electronics"), filters);
    }

    [Fact]
    public void ContainsAny_MultipleValues_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("tags").ContainsAny("sale", "featured", "new");

        Assert.Contains(new KeyValuePair<string, string>("elements.tags[any]", "sale,featured,new"), filters);
    }

    [Fact]
    public void ContainsAll_MultipleValues_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("required_tags").ContainsAll("premium", "verified");

        Assert.Contains(new KeyValuePair<string, string>("elements.required_tags[all]", "premium,verified"), filters);
    }

    #endregion

    #region Empty/NotEmpty Operators

    [Fact]
    public void Empty_UsesKeyAsPathAndValueAsSuffix()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("title").IsEmpty();

        Assert.Contains(new KeyValuePair<string, string>("elements.title", "[empty]"), filters);
    }

    [Fact]
    public void IsNotEmpty_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("description").IsNotEmpty();

        Assert.Contains(new KeyValuePair<string, string>("elements.description", "[nempty]"), filters);
    }

    #endregion

    #region Range Operators

    [Fact]
    public void Range_UsesInclusiveBounds()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("date").IsWithinRange(DateTime.Parse("2020-01-01"), DateTime.Parse("2020-12-31"));

        Assert.Contains(
            new KeyValuePair<string, string>("elements.date[range]", "2020-01-01T00:00:00Z,2020-12-31T00:00:00Z"),
            filters);
    }

    [Fact]
    public void Range_Numbers_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("price").IsWithinRange(10.0, 100.0);

        Assert.Contains(new KeyValuePair<string, string>("elements.price[range]", "10,100"), filters);
    }

    #endregion

    #region System Properties

    [Fact]
    public void System_Type_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.System("type").IsEqualTo("article");

        Assert.Contains(new KeyValuePair<string, string>("system.type[eq]", "article"), filters);
    }

    [Fact]
    public void System_Language_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.System("language").IsEqualTo("en-US");

        Assert.Contains(new KeyValuePair<string, string>("system.language[eq]", "en-US"), filters);
    }

    [Fact]
    public void System_Codename_In_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.System("codename").IsIn("article_1", "article_2", "article_3");

        Assert.Contains(new KeyValuePair<string, string>("system.codename[in]", "article_1,article_2,article_3"), filters);
    }

    [Fact]
    public void System_Collection_ProducesCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.System("collection").IsEqualTo("blog");

        Assert.Contains(new KeyValuePair<string, string>("system.collection[eq]", "blog"), filters);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void DuplicateKeys_AreAllowed()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.System("type").IsEqualTo("article");
        f.System("type").IsEqualTo("article");

        Assert.Equal(2, filters.Count(kvp => kvp.Key == "system.type[eq]" && kvp.Value == "article"));
    }

    [Fact]
    public void MultipleFilters_CanBeChained()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.System("type").IsEqualTo("article");
        f.Element("category").Contains("news");
        f.Element("publish_date").IsGreaterThan(DateTime.Parse("2024-01-01"));

        Assert.Equal(3, filters.Count);
        Assert.Contains(new KeyValuePair<string, string>("system.type[eq]", "article"), filters);
        Assert.Contains(new KeyValuePair<string, string>("elements.category[contains]", "news"), filters);
        Assert.Contains(new KeyValuePair<string, string>("elements.publish_date[gt]", "2024-01-01T00:00:00Z"), filters);
    }

    [Fact]
    public void SpecialCharacters_InValues_AreUrlEncoded()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("title").IsEqualTo("Hello & World");

        // Values are URL-encoded by the filter builder
        Assert.Contains(new KeyValuePair<string, string>("elements.title[eq]", "Hello%20%26%20World"), filters);
    }

    #endregion
}
