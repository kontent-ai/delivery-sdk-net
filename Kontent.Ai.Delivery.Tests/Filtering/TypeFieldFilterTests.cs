using Kontent.Ai.Delivery.Api.Filtering;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class TypeFieldFilterTests
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
        var (filters, sut) = CreateFilter("system.type");

        sut.IsNotEqualTo("article");

        Assert.Single(filters);
        Assert.Equal("system.type[neq]", filters[0].Key);
        Assert.Equal("article", filters[0].Value);
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

    [Fact]
    public void IsIn_AddsInFilter()
    {
        var (filters, sut) = CreateFilter("system.type");

        sut.IsIn("article", "blog_post");

        Assert.Single(filters);
        Assert.Equal("system.type[in]", filters[0].Key);
        Assert.Equal("article,blog_post", filters[0].Value);
    }

    [Fact]
    public void IsNotIn_AddsNinFilter()
    {
        var (filters, sut) = CreateFilter("system.type");

        sut.IsNotIn("landing_page");

        Assert.Single(filters);
        Assert.Equal("system.type[nin]", filters[0].Key);
        Assert.Equal("landing_page", filters[0].Value);
    }

    [Fact]
    public void IsWithinRange_AddsRangeFilter()
    {
        var (filters, sut) = CreateFilter("system.last_modified");
        var lower = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var upper = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        sut.IsWithinRange(lower, upper);

        Assert.Single(filters);
        Assert.Equal("system.last_modified[range]", filters[0].Key);
        Assert.Equal("2024-01-01T00:00:00Z,2024-12-31T23:59:59Z", filters[0].Value);
    }

    [Fact]
    public void IsLessThan_AddsLtFilter()
    {
        var (filters, sut) = CreateFilter("system.last_modified");
        var date = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        sut.IsLessThan(date);

        Assert.Single(filters);
        Assert.Equal("system.last_modified[lt]", filters[0].Key);
        Assert.Equal("2024-06-01T00:00:00Z", filters[0].Value);
    }

    [Fact]
    public void IsLessThanOrEqualTo_AddsLteFilter()
    {
        var (filters, sut) = CreateFilter("system.last_modified");
        var date = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        sut.IsLessThanOrEqualTo(date);

        Assert.Single(filters);
        Assert.Equal("system.last_modified[lte]", filters[0].Key);
        Assert.Equal("2024-06-01T00:00:00Z", filters[0].Value);
    }

    [Fact]
    public void IsGreaterThan_AddsGtFilter()
    {
        var (filters, sut) = CreateFilter("system.last_modified");
        var date = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        sut.IsGreaterThan(date);

        Assert.Single(filters);
        Assert.Equal("system.last_modified[gt]", filters[0].Key);
        Assert.Equal("2024-06-01T00:00:00Z", filters[0].Value);
    }

    [Fact]
    public void IsGreaterThanOrEqualTo_AddsGteFilter()
    {
        var (filters, sut) = CreateFilter("system.last_modified");
        var date = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        sut.IsGreaterThanOrEqualTo(date);

        Assert.Single(filters);
        Assert.Equal("system.last_modified[gte]", filters[0].Key);
        Assert.Equal("2024-06-01T00:00:00Z", filters[0].Value);
    }

    private static (List<KeyValuePair<string, string>> filters, TypeFieldFilter<string> sut) CreateFilter(string path)
    {
        var filters = new List<KeyValuePair<string, string>>();
        var sut = new TypeFieldFilter<string>("builder", path, filters.Add);
        return (filters, sut);
    }
}
