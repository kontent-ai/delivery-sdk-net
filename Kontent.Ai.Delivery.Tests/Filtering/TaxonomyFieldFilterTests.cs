using Kontent.Ai.Delivery.Api.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class TaxonomyFieldFilterTests
{
    [Fact]
    public void IsEqualTo_DateTime_AddsEqFilter()
    {
        var (filters, sut) = CreateFilter("system.last_modified");
        var date = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        sut.IsEqualTo(date);

        Assert.Single(filters);
        Assert.Equal("system.last_modified[eq]", filters[0].Key);
        Assert.Equal("2024-06-15T12:00:00Z", filters[0].Value);
    }

    [Fact]
    public void IsNotEqualTo_String_AddsNeqFilter()
    {
        var (filters, sut) = CreateFilter("system.codename");

        sut.IsNotEqualTo("taxonomy_a");

        Assert.Single(filters);
        Assert.Equal("system.codename[neq]", filters[0].Key);
        Assert.Equal("taxonomy_a", filters[0].Value);
    }

    [Fact]
    public void IsNotEqualTo_DateTime_AddsNeqFilter()
    {
        var (filters, sut) = CreateFilter("system.last_modified");
        var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        sut.IsNotEqualTo(date);

        Assert.Single(filters);
        Assert.Equal("system.last_modified[neq]", filters[0].Key);
        Assert.Equal("2024-01-01T00:00:00Z", filters[0].Value);
    }

    private static (List<KeyValuePair<string, string>> filters, TaxonomyFieldFilter<string> sut) CreateFilter(string path)
    {
        var filters = new List<KeyValuePair<string, string>>();
        var sut = new TaxonomyFieldFilter<string>("builder", path, filters.Add);
        return (filters, sut);
    }
}
