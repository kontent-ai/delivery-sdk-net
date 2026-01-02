using Kontent.Ai.Delivery.Api.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class FilterTests
{
    [Fact]
    public void Empty_UsesKeyAsPathAndValueAsSuffix()
    {
        var dict = new Dictionary<string, string>();
        var f = new ItemsFilterBuilder(dict);

        f.Element("title").Empty();

        Assert.Equal("[empty]", dict["elements.title"]);
    }

    [Fact]
    public void In_UsesCommaSeparatedValues()
    {
        var dict = new Dictionary<string, string>();
        var f = new ItemsFilterBuilder(dict);

        f.Element("tags").In("a", "b");

        Assert.Equal("a,b", dict["elements.tags[in]"]);
    }

    [Fact]
    public void Range_UsesInclusiveBounds()
    {
        var dict = new Dictionary<string, string>();
        var f = new ItemsFilterBuilder(dict);

        f.Element("date").Range(DateTime.Parse("2020-01-01"), DateTime.Parse("2020-12-31"));

        Assert.Equal("2020-01-01T00:00:00Z,2020-12-31T00:00:00Z", dict["elements.date[range]"]);
    }
}