using Kontent.Ai.Delivery.Api.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class ItemFiltersTests
{
    [Fact]
    public void Eq_String_BuildsCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.System("type").IsEqualTo("article");

        Assert.Contains(new KeyValuePair<string, string>("system.type[eq]", "article"), filters);
    }

    [Fact]
    public void Eq_Number_BuildsCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("price").IsEqualTo(99.99);

        Assert.Contains(new KeyValuePair<string, string>("elements.price[eq]", "99.99"), filters);
    }

    [Fact]
    public void Eq_DateTime_BuildsCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);
        var date = new DateTime(2024, 8, 14, 10, 30, 0, DateTimeKind.Utc);

        f.Element("publish_date").IsEqualTo(date);

        Assert.Contains(new KeyValuePair<string, string>("elements.publish_date[eq]", "2024-08-14T10:30:00Z"), filters);
    }

    [Fact]
    public void Contains_EncodesCorrectly()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        // Delivery API [contains] is only valid for arrays (taxonomy/linked items/multiple choice/custom elements),
        // not strings. Use an array-like element codename in examples.
        f.Element("category").Contains("coffee & tea");

        Assert.Contains("&", filters.Single(kvp => kvp.Key == "elements.category[contains]").Value);
    }

    [Fact]
    public void Range_Numeric_BuildsCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("price").IsWithinRange(10.0, 100.0);

        Assert.Contains(new KeyValuePair<string, string>("elements.price[range]", "10,100"), filters);
    }

    [Fact]
    public void Empty_BuildsCorrectFilter()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("description").IsEmpty();

        Assert.Contains(new KeyValuePair<string, string>("elements.description[empty]", string.Empty), filters);
    }
}
