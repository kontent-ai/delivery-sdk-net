using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryParameters.Filtering;

public class ItemFiltersTests
{
    private readonly ItemFilters _filters = new();

    #region Equality Operators

    [Theory]
    [InlineData("article", "system.type[eq]=article")]
    [InlineData("blog_post", "system.type[eq]=blog_post")]
    public void Equals_String_BuildsCorrectFilter(string value, string expected)
    {
        var filter = _filters.Equals(ItemSystemPath.Type, value);
        var (key, val) = filter.ToQueryParameter();
        Assert.Equal(expected, $"{key}={val}");
    }

    [Fact]
    public void Equals_Number_BuildsCorrectFilter()
    {
        var filter = _filters.Equals(Elements.GetPath("price"), 99.99);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.price[eq]=99.99", $"{key}={value}");
    }

    [Fact]
    public void Equals_DateTime_BuildsCorrectFilter()
    {
        var date = new DateTime(2024, 8, 14, 10, 30, 0, DateTimeKind.Utc);
        var filter = _filters.Equals(Elements.GetPath("publish_date"), date);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.publish_date[eq]=2024-08-14T10:30:00Z", $"{key}={value}");
    }

    [Theory]
    [InlineData(true, "elements.featured[eq]=true")]
    [InlineData(false, "elements.featured[eq]=false")]
    public void Equals_Boolean_BuildsCorrectFilter(bool boolValue, string expected)
    {
        var filter = _filters.Equals(Elements.GetPath("featured"), boolValue);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal(expected, $"{key}={value}");
    }

    [Fact]
    public void NotEquals_String_BuildsCorrectFilter()
    {
        var filter = _filters.NotEquals(ItemSystemPath.Type, "draft");
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("system.type[neq]=draft", $"{key}={value}");
    }

    #endregion

    #region Comparison Operators

    [Fact]
    public void LessThan_Number_BuildsCorrectFilter()
    {
        var filter = _filters.LessThan(Elements.GetPath("price"), 100.0);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.price[lt]=100", $"{key}={value}");
    }

    [Fact]
    public void LessThan_DateTime_BuildsCorrectFilter()
    {
        var date = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var filter = _filters.LessThan(ItemSystemPath.LastModified, date);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("system.last_modified[lt]=2024-12-31T23:59:59Z", $"{key}={value}");
    }

    [Fact]
    public void LessThanOrEqual_Number_BuildsCorrectFilter()
    {
        var filter = _filters.LessThanOrEqual(Elements.GetPath("quantity"), 50.0);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.quantity[lte]=50", $"{key}={value}");
    }

    [Fact]
    public void GreaterThan_Number_BuildsCorrectFilter()
    {
        var filter = _filters.GreaterThan(Elements.GetPath("rating"), 4.0);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.rating[gt]=4", $"{key}={value}");
    }

    [Fact]
    public void GreaterThan_DateTime_BuildsCorrectFilter()
    {
        var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var filter = _filters.GreaterThan(ItemSystemPath.LastModified, date);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("system.last_modified[gt]=2024-01-01T00:00:00Z", $"{key}={value}");
    }

    [Fact]
    public void GreaterThanOrEqual_Number_BuildsCorrectFilter()
    {
        var filter = _filters.GreaterThanOrEqual(Elements.GetPath("min_age"), 18.0);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.min_age[gte]=18", $"{key}={value}");
    }

    #endregion

    #region Range Operator

    [Fact]
    public void Range_NumericTuple_BuildsCorrectFilter()
    {
        var filter = _filters.Range(Elements.GetPath("price"), (10.0, 100.0));
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.price[range]=10,100", $"{key}={value}");
    }

    [Fact]
    public void Range_DateTimeTuple_BuildsCorrectFilter()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var filter = _filters.Range(ItemSystemPath.LastModified, (start, end));
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("system.last_modified[range]=2024-01-01T00:00:00Z,2024-12-31T23:59:59Z", $"{key}={value}");
    }

    #endregion

    #region Contains Operator

    [Fact]
    public void Contains_String_BuildsCorrectFilter()
    {
        var filter = _filters.Contains(Elements.GetPath("title"), "coffee");
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.title[contains]=coffee", $"{key}={value}");
    }

    [Fact]
    public void Contains_SpecialCharacters_EncodesCorrectly()
    {
        var filter = _filters.Contains(Elements.GetPath("title"), "coffee & tea");
        var (key, value) = filter.ToQueryParameter();
        Assert.Contains("%26", value); // & should be encoded
    }

    #endregion

    #region Array Operators - In/NotIn

    [Fact]
    public void In_StringArray_BuildsCorrectFilter()
    {
        var filter = _filters.In(Elements.GetPath("category"), ["tech", "programming"]);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.category[in]=tech,programming", $"{key}={value}");
    }

    [Fact]
    public void In_NumericArray_BuildsCorrectFilter()
    {
        var filter = _filters.In(Elements.GetPath("rating"), [4.0, 5.0]);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.rating[in]=4,5", $"{key}={value}");
    }

    [Fact]
    public void In_DateTimeArray_BuildsCorrectFilter()
    {
        var dates = new[]
        {
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc)
        };
        var filter = _filters.In(Elements.GetPath("event_date"), dates);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.event_date[in]=2024-01-01T00:00:00Z,2024-12-31T00:00:00Z", $"{key}={value}");
    }

    [Fact]
    public void NotIn_StringArray_BuildsCorrectFilter()
    {
        var filter = _filters.NotIn(ItemSystemPath.WorkflowStep, ["draft", "archived"]);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("system.workflow_step[nin]=draft,archived", $"{key}={value}");
    }

    [Fact]
    public void NotIn_NumericArray_BuildsCorrectFilter()
    {
        var filter = _filters.NotIn(Elements.GetPath("status_code"), [404.0, 500.0]);
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.status_code[nin]=404,500", $"{key}={value}");
    }

    #endregion

    #region Array Operators - Any/All

    [Fact]
    public void Any_Params_BuildsCorrectFilter()
    {
        var filter = _filters.Any(Elements.GetPath("tags"), "featured", "trending", "popular");
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.tags[any]=featured,trending,popular", $"{key}={value}");
    }

    [Fact]
    public void Any_SingleValue_BuildsCorrectFilter()
    {
        var filter = _filters.Any(Elements.GetPath("tags"), "featured");
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.tags[any]=featured", $"{key}={value}");
    }

    [Fact]
    public void All_Params_BuildsCorrectFilter()
    {
        var filter = _filters.All(Elements.GetPath("required_features"), "warranty", "manual", "support");
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.required_features[all]=warranty,manual,support", $"{key}={value}");
    }

    #endregion

    #region Empty/NotEmpty Operators

    [Fact]
    public void Empty_BuildsCorrectFilter()
    {
        var filter = _filters.Empty(Elements.GetPath("description"));
        var (key, value) = filter.ToQueryParameter();
        // Empty operators have special serialization where value is the operator suffix
        Assert.Equal("elements.description[empty]", $"{key}{value}");
    }

    [Fact]
    public void NotEmpty_BuildsCorrectFilter()
    {
        var filter = _filters.NotEmpty(Elements.GetPath("thumbnail"));
        var (key, value) = filter.ToQueryParameter();
        Assert.Equal("elements.thumbnail[nempty]", $"{key}{value}");
    }

    #endregion

    #region Property Path Tests

    [Fact]
    public void Filter_SystemProperty_UsesCorrectPath()
    {
        var filter = _filters.Equals(ItemSystemPath.Collection, "news");
        var (key, _) = filter.ToQueryParameter();
        Assert.StartsWith("system.collection", key);
    }

    [Fact]
    public void Filter_ElementProperty_UsesCorrectPath()
    {
        var filter = _filters.Equals(Elements.GetPath("custom_field"), "value");
        var (key, _) = filter.ToQueryParameter();
        Assert.StartsWith("elements.custom_field", key);
    }

    [Fact]
    public void Filter_ComplexElementCodename_BuildsCorrectly()
    {
        var filter = _filters.Equals(Elements.GetPath("my_complex_element_name"), "value");
        var (key, _) = filter.ToQueryParameter();
        Assert.StartsWith("elements.my_complex_element_name", key);
    }

    #endregion
}
