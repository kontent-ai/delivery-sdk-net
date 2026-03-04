using Kontent.Ai.Delivery.Api.Filtering;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class FilterValidationTests
{
    [Fact]
    public void FilterPath_ThrowsOnNullOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => FilterPath.System(null!));
        Assert.Throws<ArgumentException>(() => FilterPath.System(" "));
        Assert.Throws<ArgumentException>(() => FilterPath.Element(null!));
        Assert.Throws<ArgumentException>(() => FilterPath.Element(" "));
    }

    [Fact]
    public void FilterPath_ThrowsOnSpaces()
    {
        Assert.Throws<ArgumentException>(() => FilterPath.System("last modified"));
        Assert.Throws<ArgumentException>(() => FilterPath.Element("seo description"));
    }

    [Fact]
    public void FilterPath_ThrowsOnWrongPrefix()
    {
        Assert.Throws<ArgumentException>(() => FilterPath.System("elements.title"));
        Assert.Throws<ArgumentException>(() => FilterPath.Element("system.type"));
    }

    [Fact]
    public void SerializeArray_ThrowsOnNullOrEmpty()
    {
        Assert.Throws<ArgumentNullException>(() => FilterValueSerializer.SerializeArray((string[])null!));
        Assert.Throws<ArgumentException>(() => FilterValueSerializer.SerializeArray(Array.Empty<string>()));
    }

    [Fact]
    public void SerializeRange_ThrowsOnInvalidBounds()
    {
        Assert.Throws<ArgumentException>(() => FilterValueSerializer.SerializeRange(10, 5));
        Assert.Throws<ArgumentException>(() => FilterValueSerializer.SerializeRange("zebra", "apple"));
    }
}
