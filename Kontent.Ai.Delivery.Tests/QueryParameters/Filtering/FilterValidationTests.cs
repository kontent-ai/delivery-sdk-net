using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryParameters.Filtering;

public class FilterValidationTests
{
    #region Filter Constructor Validation

    [Fact]
    public void Filter_ThrowsOnNullPropertyPath()
    {
        Assert.Throws<ArgumentException>(() =>
            new Filter(null!, FilterOperator.Equals, StringValue.From("value")));
    }

    [Fact]
    public void Filter_ThrowsOnEmptyPropertyPath()
    {
        Assert.Throws<ArgumentException>(() =>
            new Filter("", FilterOperator.Equals, StringValue.From("value")));
    }

    [Fact]
    public void Filter_ThrowsOnWhitespacePropertyPath()
    {
        Assert.Throws<ArgumentException>(() =>
            new Filter("   ", FilterOperator.Equals, StringValue.From("value")));
    }

    [Fact]
    public void Filter_Empty_ThrowsWhenValueProvided()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Filter("elements.title", FilterOperator.Empty, StringValue.From("value")));

        Assert.Contains("Empty operator must not have a value", ex.Message);
    }

    [Fact]
    public void Filter_NotEmpty_ThrowsWhenValueProvided()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Filter("elements.title", FilterOperator.NotEmpty, StringValue.From("value")));

        Assert.Contains("NotEmpty operator must not have a value", ex.Message);
    }

    [Fact]
    public void Filter_Empty_AcceptsNullValue()
    {
        var filter = new Filter("elements.title", FilterOperator.Empty, null);
        Assert.NotNull(filter);
    }

    [Fact]
    public void Filter_NotEmpty_AcceptsNullValue()
    {
        var filter = new Filter("elements.title", FilterOperator.NotEmpty, null);
        Assert.NotNull(filter);
    }

    [Theory]
    [InlineData(FilterOperator.Equals)]
    [InlineData(FilterOperator.NotEquals)]
    [InlineData(FilterOperator.LessThan)]
    [InlineData(FilterOperator.GreaterThan)]
    [InlineData(FilterOperator.Range)]
    [InlineData(FilterOperator.In)]
    [InlineData(FilterOperator.Contains)]
    [InlineData(FilterOperator.Any)]
    [InlineData(FilterOperator.All)]
    public void Filter_NonEmptyOperators_ThrowWhenValueIsNull(FilterOperator op)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Filter("elements.title", op, null));

        Assert.Contains($"{op} operator requires a value", ex.Message);
    }

    #endregion

    #region Operator Serialization

    [Theory]
    [InlineData(FilterOperator.Equals, "[eq]")]
    [InlineData(FilterOperator.NotEquals, "[neq]")]
    [InlineData(FilterOperator.LessThan, "[lt]")]
    [InlineData(FilterOperator.LessThanOrEqual, "[lte]")]
    [InlineData(FilterOperator.GreaterThan, "[gt]")]
    [InlineData(FilterOperator.GreaterThanOrEqual, "[gte]")]
    [InlineData(FilterOperator.Range, "[range]")]
    [InlineData(FilterOperator.In, "[in]")]
    [InlineData(FilterOperator.NotIn, "[nin]")]
    [InlineData(FilterOperator.Contains, "[contains]")]
    [InlineData(FilterOperator.Any, "[any]")]
    [InlineData(FilterOperator.All, "[all]")]
    public void Filter_SerializesOperatorCorrectly(FilterOperator op, string expectedSuffix)
    {
        var filter = new Filter("elements.field", op, StringValue.From("test"));
        var (key, _) = filter.ToQueryParameter();

        Assert.EndsWith(expectedSuffix, key);
    }

    [Theory]
    [InlineData(FilterOperator.Empty, "[empty]")]
    [InlineData(FilterOperator.NotEmpty, "[nempty]")]
    public void Filter_SerializesEmptyOperatorsCorrectly(FilterOperator op, string expectedSuffix)
    {
        var filter = new Filter("elements.field", op, null);
        var (key, value) = filter.ToQueryParameter();

        // Empty operators serialize as: key="elements.field", value="[empty]"
        Assert.Equal("elements.field", key);
        Assert.Equal(expectedSuffix, value);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void Filter_PropertyPath_PreservesExactValue()
    {
        var filter = new Filter("system.workflow_step", FilterOperator.Equals, StringValue.From("published"));
        var (key, _) = filter.ToQueryParameter();

        Assert.StartsWith("system.workflow_step", key);
    }

    [Fact]
    public void Filter_ValueWithSpecialCharacters_EncodesCorrectly()
    {
        var filter = new Filter("elements.title", FilterOperator.Equals, StringValue.From("Hello & Goodbye"));
        var (_, value) = filter.ToQueryParameter();

        Assert.Contains("%26", value); // & is encoded
    }

    [Fact]
    public void Filter_MultipleFilters_EachHasUniqueKey()
    {
        var filter1 = new Filter("system.type", FilterOperator.Equals, StringValue.From("article"));
        var filter2 = new Filter("elements.title", FilterOperator.Contains, StringValue.From("test"));

        var (key1, _) = filter1.ToQueryParameter();
        var (key2, _) = filter2.ToQueryParameter();

        Assert.NotEqual(key1, key2);
    }

    #endregion
}
