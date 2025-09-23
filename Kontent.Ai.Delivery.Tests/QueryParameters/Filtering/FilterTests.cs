using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryParameters.Filtering;

public class FilterTests
{
    [Theory]
    [InlineData("elements.title", FilterOperator.Equals, "test", "elements.title[eq]=test")]
    [InlineData("system.type", FilterOperator.Equals, "article", "system.type[eq]=article")]
    public void ToQueryParameter_ScalarOperators(string path, FilterOperator op, string value, string expected)
    {
        var filter = new Filter(path, op, StringValue.From(value));

        var result = filter.ToQueryParameter();

        Assert.Equal(expected, $"{result.Key}={result.Value}");
    }

    [Fact]
    public void ToQueryParameter_Empty_NoValueSuffix()
    {
        var filter = new Filter("elements.title", FilterOperator.Empty, EmptyValue.From(string.Empty));

        var result = filter.ToQueryParameter();

        Assert.Equal("elements.title[empty]", $"{result.Key}={result.Value}");
    }

    [Fact]
    public void ToQueryParameter_In_StringArray()
    {
        var filter = new Filter("elements.tags", FilterOperator.In, StringArrayValue.From(["a", "b"]));

        var result = filter.ToQueryParameter();

        Assert.Equal("elements.tags[in]=a,b", $"{result.Key}={result.Value}");
    }

    [Fact]
    public void ToQueryParameter_Range_Inclusive()
    {
        var filter = new Filter("elements.date", FilterOperator.Range, FilterValueMapper.From((DateTime.Parse("2020-01-01"), DateTime.Parse("2020-12-31"))));

        var result = filter.ToQueryParameter();

        Assert.Equal("elements.date[range]=2020-01-01,2020-12-31", $"{result.Key}={result.Value}");
    }
}


