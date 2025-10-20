using System;
using System.Globalization;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryParameters.Filtering;

public class FilterValueTests
{
    #region StringValue Tests

    [Fact]
    public void StringValue_Serializes_UrlEncoded()
    {
        var value = StringValue.From("hello world");
        Assert.Equal("hello%20world", value.Serialize());
    }

    [Fact]
    public void StringValue_Serializes_SpecialCharacters()
    {
        var value = StringValue.From("coffee & tea");
        Assert.Equal("coffee%20%26%20tea", value.Serialize());
    }

    [Fact]
    public void StringValue_Serializes_UnicodeCharacters()
    {
        var value = StringValue.From("café naïve");
        Assert.Contains("%", value.Serialize()); // URL encoded
    }

    [Fact]
    public void StringValue_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => StringValue.From(null!));
    }

    #endregion

    #region NumericValue Tests

    [Fact]
    public void NumericValue_Serializes_Integer()
    {
        var value = NumericValue.From(42);
        Assert.Equal("42", value.Serialize());
    }

    [Fact]
    public void NumericValue_Serializes_Decimal()
    {
        var value = NumericValue.From(42.5);
        Assert.Equal("42.5", value.Serialize());
    }

    [Fact]
    public void NumericValue_Serializes_InvariantCulture()
    {
        var currentCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // Uses comma as decimal separator
            var value = NumericValue.From(42.5);
            Assert.Equal("42.5", value.Serialize()); // Should still use dot
        }
        finally
        {
            CultureInfo.CurrentCulture = currentCulture;
        }
    }

    [Fact]
    public void NumericValue_Serializes_NegativeNumber()
    {
        var value = NumericValue.From(-100.5);
        Assert.Equal("-100.5", value.Serialize());
    }

    #endregion

    #region DateTimeValue Tests

    [Fact]
    public void DateTimeValue_Serializes_IsoFormat()
    {
        var date = new DateTime(2024, 8, 14, 10, 30, 0, DateTimeKind.Utc);
        var value = DateTimeValue.From(date);
        Assert.Equal("2024-08-14T10:30:00Z", value.Serialize());
    }

    [Fact]
    public void DateTimeValue_Serializes_DateOnly()
    {
        var date = new DateTime(2024, 1, 1);
        var value = DateTimeValue.From(date);
        Assert.Equal("2024-01-01T00:00:00Z", value.Serialize());
    }

    #endregion

    #region BooleanValue Tests

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void BooleanValue_Serializes_Lowercase(bool input, string expected)
    {
        var value = BooleanValue.From(input);
        Assert.Equal(expected, value.Serialize());
    }

    #endregion

    #region StringArrayValue Tests

    [Fact]
    public void StringArrayValue_Serializes_CommaSeparated()
    {
        var value = StringArrayValue.From(["a", "b", "c"]);
        Assert.Equal("a,b,c", value.Serialize());
    }

    [Fact]
    public void StringArrayValue_Serializes_UrlEncodedValues()
    {
        var value = StringArrayValue.From(["hello world", "foo bar"]);
        Assert.Equal("hello%20world,foo%20bar", value.Serialize());
    }

    [Fact]
    public void StringArrayValue_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => StringArrayValue.From(null!));
    }

    [Fact]
    public void StringArrayValue_ThrowsOnEmptyArray()
    {
        Assert.Throws<ArgumentException>(() => StringArrayValue.From([]));
    }

    [Fact]
    public void StringArrayValue_Equals_ComparesSequence()
    {
        var value1 = StringArrayValue.From(["a", "b"]);
        var value2 = StringArrayValue.From(["a", "b"]);
        var value3 = StringArrayValue.From(["b", "a"]);

        Assert.Equal(value1, value2);
        Assert.NotEqual(value1, value3);
    }

    #endregion

    #region NumericArrayValue Tests

    [Fact]
    public void NumericArrayValue_Serializes_CommaSeparated()
    {
        var value = NumericArrayValue.From([1.0, 2.5, 3.0]);
        Assert.Equal("1,2.5,3", value.Serialize());
    }

    [Fact]
    public void NumericArrayValue_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => NumericArrayValue.From(null!));
    }

    [Fact]
    public void NumericArrayValue_ThrowsOnEmptyArray()
    {
        Assert.Throws<ArgumentException>(() => NumericArrayValue.From([]));
    }

    #endregion

    #region DateTimeArrayValue Tests

    [Fact]
    public void DateTimeArrayValue_Serializes_IsoFormat()
    {
        var dates = new[]
        {
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc)
        };
        var value = DateTimeArrayValue.From(dates);
        Assert.Equal("2024-01-01T00:00:00Z,2024-12-31T23:59:59Z", value.Serialize());
    }

    [Fact]
    public void DateTimeArrayValue_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => DateTimeArrayValue.From(null!));
    }

    [Fact]
    public void DateTimeArrayValue_ThrowsOnEmptyArray()
    {
        Assert.Throws<ArgumentException>(() => DateTimeArrayValue.From([]));
    }

    #endregion

    #region NumericRangeValue Tests

    [Fact]
    public void NumericRangeValue_Serializes_CommaSeparated()
    {
        var value = NumericRangeValue.From((10.0, 100.0));
        Assert.Equal("10,100", value.Serialize());
    }

    [Fact]
    public void NumericRangeValue_AllowsEqualBounds()
    {
        var value = NumericRangeValue.From((50.0, 50.0));
        Assert.Equal("50,50", value.Serialize());
    }

    [Fact]
    public void NumericRangeValue_ThrowsOnInvalidRange()
    {
        Assert.Throws<ArgumentException>(() => NumericRangeValue.From((100.0, 50.0)));
    }

    #endregion

    #region DateRangeValue Tests

    [Fact]
    public void DateRangeValue_Serializes_IsoFormat()
    {
        var range = (
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc)
        );
        var value = DateRangeValue.From(range);
        Assert.Equal("2024-01-01T00:00:00Z,2024-12-31T23:59:59Z", value.Serialize());
    }

    [Fact]
    public void DateRangeValue_AllowsEqualBounds()
    {
        var date = new DateTime(2024, 6, 15);
        var value = DateRangeValue.From((date, date));
        Assert.Contains("2024-06-15", value.Serialize());
    }

    [Fact]
    public void DateRangeValue_ThrowsOnInvalidRange()
    {
        var start = new DateTime(2024, 12, 31);
        var end = new DateTime(2024, 1, 1);
        Assert.Throws<ArgumentException>(() => DateRangeValue.From((start, end)));
    }

    #endregion

    #region StringRangeValue Tests

    [Fact]
    public void StringRangeValue_Serializes_UrlEncoded()
    {
        var value = StringRangeValue.From(("apple", "zebra"));
        Assert.Equal("apple,zebra", value.Serialize());
    }

    [Fact]
    public void StringRangeValue_AllowsEqualBounds()
    {
        var value = StringRangeValue.From(("test", "test"));
        Assert.Equal("test,test", value.Serialize());
    }

    [Fact]
    public void StringRangeValue_ThrowsOnInvalidRange()
    {
        Assert.Throws<ArgumentException>(() => StringRangeValue.From(("zebra", "apple")));
    }

    [Fact]
    public void StringRangeValue_ThrowsOnNullLower()
    {
        Assert.Throws<ArgumentNullException>(() => StringRangeValue.From((null!, "upper")));
    }

    [Fact]
    public void StringRangeValue_ThrowsOnNullUpper()
    {
        Assert.Throws<ArgumentNullException>(() => StringRangeValue.From(("lower", null!)));
    }

    #endregion
}
