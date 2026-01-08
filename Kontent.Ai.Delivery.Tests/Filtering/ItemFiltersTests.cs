using Kontent.Ai.Delivery.Api.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class ItemFiltersTests
{
    [Fact]
    public void Eq_String_BuildsCorrectFilter()
    {
        var dict = new Dictionary<string, string>();
        var f = new ItemsFilterBuilder(dict);

        f.System("type").IsEqualTo("article");

        Assert.Equal("article", dict["system.type[eq]"]);
    }

    [Fact]
    public void Eq_Number_BuildsCorrectFilter()
    {
        var dict = new Dictionary<string, string>();
        var f = new ItemsFilterBuilder(dict);

        f.Element("price").IsEqualTo(99.99);

        Assert.Equal("99.99", dict["elements.price[eq]"]);
    }

    [Fact]
    public void Eq_DateTime_BuildsCorrectFilter()
    {
        var dict = new Dictionary<string, string>();
        var f = new ItemsFilterBuilder(dict);
        var date = new DateTime(2024, 8, 14, 10, 30, 0, DateTimeKind.Utc);

        f.Element("publish_date").IsEqualTo(date);

        Assert.Equal("2024-08-14T10:30:00Z", dict["elements.publish_date[eq]"]);
    }

    [Fact]
    public void Contains_EncodesCorrectly()
    {
        var dict = new Dictionary<string, string>();
        var f = new ItemsFilterBuilder(dict);

        // Delivery API [contains] is only valid for arrays (taxonomy/linked items/multiple choice/custom elements),
        // not strings. Use an array-like element codename in examples.
        f.Element("category").Contains("coffee & tea");

        Assert.Contains("%26", dict["elements.category[contains]"]);
    }

    [Fact]
    public void Range_Numeric_BuildsCorrectFilter()
    {
        var dict = new Dictionary<string, string>();
        var f = new ItemsFilterBuilder(dict);

        f.Element("price").IsWithinRange(10.0, 100.0);

        Assert.Equal("10,100", dict["elements.price[range]"]);
    }

    [Fact]
    public void Empty_BuildsCorrectFilter()
    {
        var dict = new Dictionary<string, string>();
        var f = new ItemsFilterBuilder(dict);

        f.Element("description").IsEmpty();

        Assert.Equal("[empty]", dict["elements.description"]);
    }
}