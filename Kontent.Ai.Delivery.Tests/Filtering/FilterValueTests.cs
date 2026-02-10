using System.Globalization;
using Kontent.Ai.Delivery.Api.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class FilterValueTests
{
    [Fact]
    public void Serialize_String_IsNotUrlEncoded()
        => Assert.Equal("hello world", FilterValueSerializer.Serialize("hello world"));

    [Fact]
    public void Serialize_String_SpecialCharacters_AreNotUrlEncoded()
        => Assert.Equal("coffee & tea", FilterValueSerializer.Serialize("coffee & tea"));

    [Fact]
    public void Serialize_String_UnicodeCharacters_AreNotUrlEncoded()
        => Assert.Equal("café naïve", FilterValueSerializer.Serialize("café naïve"));

    [Fact]
    public void Serialize_String_ThrowsOnNull()
        => Assert.Throws<ArgumentNullException>(() => FilterValueSerializer.Serialize((string)null!));

    [Fact]
    public void Serialize_Number_InvariantCulture()
    {
        var currentCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal("42.5", FilterValueSerializer.Serialize(42.5));
        }
        finally
        {
            CultureInfo.CurrentCulture = currentCulture;
        }
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void Serialize_Boolean_Lowercase(bool input, string expected)
        => Assert.Equal(expected, FilterValueSerializer.Serialize(input));

    [Fact]
    public void Serialize_DateTime_IsoFormat()
    {
        var date = new DateTime(2024, 8, 14, 10, 30, 0, DateTimeKind.Utc);
        Assert.Equal("2024-08-14T10:30:00Z", FilterValueSerializer.Serialize(date));
    }

    [Fact]
    public void SerializeArray_String_CommaSeparated_NotUrlEncoded()
    {
        var serialized = FilterValueSerializer.SerializeArray(["hello world", "foo bar"]);
        Assert.Equal("hello world,foo bar", serialized);
    }

    [Fact]
    public void SerializeRange_Number_ValidatesBounds()
    {
        Assert.Equal("10,100", FilterValueSerializer.SerializeRange(10.0, 100.0));
        Assert.Throws<ArgumentException>(() => FilterValueSerializer.SerializeRange(100.0, 50.0));
    }

    [Fact]
    public void SerializeRange_String_ValidatesLexicographicOrder()
    {
        Assert.Equal("apple,zebra", FilterValueSerializer.SerializeRange("apple", "zebra"));
        Assert.Throws<ArgumentException>(() => FilterValueSerializer.SerializeRange("zebra", "apple"));
    }

    [Fact]
    public void FilterValueSerializer_DateRange_NormalizesUtcBeforeComparison()
    {
        var localLower = new DateTime(2024, 8, 14, 10, 30, 0, DateTimeKind.Local);
        var utcUpper = localLower.ToUniversalTime();

        var serialized = FilterValueSerializer.SerializeRange(localLower, utcUpper);

        Assert.Equal(
            $"{FilterValueSerializer.Serialize(localLower)},{FilterValueSerializer.Serialize(utcUpper)}",
            serialized);
    }
}
