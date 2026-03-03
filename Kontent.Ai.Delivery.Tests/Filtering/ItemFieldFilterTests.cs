using Kontent.Ai.Delivery.Api.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class ItemFieldFilterTests
{
    private static readonly DateTime TestDate = new(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    private const string DateString = "2024-06-15T12:00:00Z";

    // IsEqualTo overloads (lines 7-9 — double, DateTime, bool are uncovered; string is covered)
    [Fact]
    public void IsEqualTo_Bool_AddsEqFilter()
    {
        var (filters, sut) = Create("elements.featured");
        sut.IsEqualTo(true);
        AssertSingle(filters, "elements.featured[eq]", "true");
    }

    // IsNotEqualTo overloads (lines 11-14 — all types)
    [Fact]
    public void IsNotEqualTo_String_AddsNeqFilter()
    {
        var (filters, sut) = Create("elements.title");
        sut.IsNotEqualTo("draft");
        AssertSingle(filters, "elements.title[neq]", "draft");
    }

    [Fact]
    public void IsNotEqualTo_Double_AddsNeqFilter()
    {
        var (filters, sut) = Create("elements.rating");
        sut.IsNotEqualTo(3.5);
        AssertSingle(filters, "elements.rating[neq]", "3.5");
    }

    [Fact]
    public void IsNotEqualTo_DateTime_AddsNeqFilter()
    {
        var (filters, sut) = Create("system.last_modified");
        sut.IsNotEqualTo(TestDate);
        AssertSingle(filters, "system.last_modified[neq]", DateString);
    }

    [Fact]
    public void IsNotEqualTo_Bool_AddsNeqFilter()
    {
        var (filters, sut) = Create("elements.featured");
        sut.IsNotEqualTo(false);
        AssertSingle(filters, "elements.featured[neq]", "false");
    }

    // IsLessThan (line 18 — string)
    [Fact]
    public void IsLessThan_String_AddsLtFilter()
    {
        var (filters, sut) = Create("elements.title");
        sut.IsLessThan("M");
        AssertSingle(filters, "elements.title[lt]", "M");
    }

    // IsLessThanOrEqualTo (lines 21-22 — DateTime, string)
    [Fact]
    public void IsLessThanOrEqualTo_DateTime_AddsLteFilter()
    {
        var (filters, sut) = Create("system.last_modified");
        sut.IsLessThanOrEqualTo(TestDate);
        AssertSingle(filters, "system.last_modified[lte]", DateString);
    }

    [Fact]
    public void IsLessThanOrEqualTo_String_AddsLteFilter()
    {
        var (filters, sut) = Create("elements.title");
        sut.IsLessThanOrEqualTo("Z");
        AssertSingle(filters, "elements.title[lte]", "Z");
    }

    // IsGreaterThan (line 26 — string)
    [Fact]
    public void IsGreaterThan_String_AddsGtFilter()
    {
        var (filters, sut) = Create("elements.title");
        sut.IsGreaterThan("A");
        AssertSingle(filters, "elements.title[gt]", "A");
    }

    // IsGreaterThanOrEqualTo (lines 29-30 — DateTime, string)
    [Fact]
    public void IsGreaterThanOrEqualTo_DateTime_AddsGteFilter()
    {
        var (filters, sut) = Create("system.last_modified");
        sut.IsGreaterThanOrEqualTo(TestDate);
        AssertSingle(filters, "system.last_modified[gte]", DateString);
    }

    [Fact]
    public void IsGreaterThanOrEqualTo_String_AddsGteFilter()
    {
        var (filters, sut) = Create("elements.title");
        sut.IsGreaterThanOrEqualTo("A");
        AssertSingle(filters, "elements.title[gte]", "A");
    }

    // IsWithinRange (line 34 — string)
    [Fact]
    public void IsWithinRange_String_AddsRangeFilter()
    {
        var (filters, sut) = Create("elements.title");
        sut.IsWithinRange("A", "M");
        AssertSingle(filters, "elements.title[range]", "A,M");
    }

    // IsIn (line 38 — DateTime)
    [Fact]
    public void IsIn_DateTime_AddsInFilter()
    {
        var (filters, sut) = Create("system.last_modified");
        sut.IsIn(TestDate);
        AssertSingle(filters, "system.last_modified[in]", DateString);
    }

    // IsNotIn (lines 40-42 — all three types)
    [Fact]
    public void IsNotIn_String_AddsNinFilter()
    {
        var (filters, sut) = Create("system.type");
        sut.IsNotIn("article");
        AssertSingle(filters, "system.type[nin]", "article");
    }

    [Fact]
    public void IsNotIn_Double_AddsNinFilter()
    {
        var (filters, sut) = Create("elements.rating");
        sut.IsNotIn(1.0, 2.0);
        AssertSingle(filters, "elements.rating[nin]", "1,2");
    }

    [Fact]
    public void IsNotIn_DateTime_AddsNinFilter()
    {
        var (filters, sut) = Create("system.last_modified");
        sut.IsNotIn(TestDate);
        AssertSingle(filters, "system.last_modified[nin]", DateString);
    }

    private static (List<KeyValuePair<string, string>> filters, ItemFieldFilter<string> sut) Create(string path)
    {
        var filters = new List<KeyValuePair<string, string>>();
        var sut = new ItemFieldFilter<string>("builder", path, filters.Add);
        return (filters, sut);
    }

    private static void AssertSingle(List<KeyValuePair<string, string>> filters, string expectedKey, string expectedValue)
    {
        Assert.Single(filters);
        Assert.Equal(expectedKey, filters[0].Key);
        Assert.Equal(expectedValue, filters[0].Value);
    }
}
