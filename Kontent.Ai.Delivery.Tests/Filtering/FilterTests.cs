using Kontent.Ai.Delivery.Api.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class FilterTests
{
    [Fact]
    public void Empty_UsesKeyAsPathAndValueAsSuffix()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("title").IsEmpty();

        Assert.Contains(new KeyValuePair<string, string>("elements.title", "[empty]"), filters);
    }

    [Fact]
    public void In_UsesCommaSeparatedValues()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.Element("tags").IsIn("a", "b");

        Assert.Contains(new KeyValuePair<string, string>("elements.tags[in]", "a,b"), filters);
    }

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
    public void DuplicateKeys_AreAllowed()
    {
        var filters = new List<KeyValuePair<string, string>>();
        var f = new ItemsFilterBuilder(filters);

        f.System("type").IsEqualTo("article");
        f.System("type").IsEqualTo("article");

        Assert.Equal(2, filters.Count(kvp => kvp.Key == "system.type[eq]" && kvp.Value == "article"));
    }
}