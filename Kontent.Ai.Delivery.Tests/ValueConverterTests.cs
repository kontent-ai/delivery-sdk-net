using Kontent.Ai.Delivery.Abstractions;
using NodaTime;
using Xunit;

namespace Kontent.Ai.Delivery.Tests;

[AttributeUsage(AttributeTargets.Property)]
public class TestGreeterValueConverterAttribute : Attribute, IElementValueConverter<string, object>
{
    public Task<object?> ConvertAsync<TElement>(TElement element, ResolvingContext context) where TElement : IContentElementValue<string>
        => Task.FromResult<object?>($"Hello {element.Value}!");
}

[AttributeUsage(AttributeTargets.Property)]
public class TestLinkedItemCodenamesValueConverterAttribute : Attribute, IElementValueConverter<List<string>, object>
{
    public Task<object?> ConvertAsync<TElement>(TElement element, ResolvingContext context) where TElement : IContentElementValue<List<string>>
        => Task.FromResult<object?>(element.Value);
}

[AttributeUsage(AttributeTargets.Property)]
public class NodaTimeValueConverterAttribute : Attribute, IElementValueConverter<DateTime?, ZonedDateTime>
{
    public Task<ZonedDateTime> ConvertAsync<TElement>(TElement element, ResolvingContext context) where TElement : IContentElementValue<DateTime?>
    {
        if (!element.Value.HasValue) return Task.FromResult<ZonedDateTime>(default);
        var udt = DateTime.SpecifyKind(element.Value.Value, DateTimeKind.Utc);
        return Task.FromResult(ZonedDateTime.FromDateTimeOffset(udt));
    }
}

public class ValueConverterTests
{
    private readonly string _guid;
    private readonly string _baseUrl;

    public ValueConverterTests()
    {
        _guid = Guid.NewGuid().ToString();
        _baseUrl = $"https://deliver.kontent.ai/{_guid}";
    }

    #region TestGreeterValueConverter Tests

    [Fact]
    public async Task TestGreeterValueConverter_ConvertsStringToGreeting()
    {
        // Arrange
        var converter = new TestGreeterValueConverterAttribute();
        var element = new TestStringElement("World");
        var context = new ResolvingContext { GetLinkedItem = _ => Task.FromResult<object>(null!) };

        // Act
        var result = await converter.ConvertAsync(element, context);

        // Assert
        Assert.Equal("Hello World!", result);
    }

    [Fact]
    public async Task TestGreeterValueConverter_HandlesEmptyString()
    {
        // Arrange
        var converter = new TestGreeterValueConverterAttribute();
        var element = new TestStringElement("");
        var context = new ResolvingContext { GetLinkedItem = _ => Task.FromResult<object>(null!) };

        // Act
        var result = await converter.ConvertAsync(element, context);

        // Assert
        Assert.Equal("Hello !", result);
    }

    [Fact]
    public async Task TestGreeterValueConverter_HandlesNullValue()
    {
        // Arrange
        var converter = new TestGreeterValueConverterAttribute();
        var element = new TestStringElement(null!);
        var context = new ResolvingContext { GetLinkedItem = _ => Task.FromResult<object>(null!) };

        // Act
        var result = await converter.ConvertAsync(element, context);

        // Assert
        Assert.Equal("Hello !", result);
    }

    #endregion

    #region TestLinkedItemCodenamesValueConverter Tests

    [Fact]
    public async Task TestLinkedItemCodenamesValueConverter_ReturnsCodenames()
    {
        // Arrange
        var converter = new TestLinkedItemCodenamesValueConverterAttribute();
        var codenames = new List<string> { "item_1", "item_2", "item_3" };
        var element = new TestLinkedItemsElement(codenames);
        var context = new ResolvingContext { GetLinkedItem = _ => Task.FromResult<object>(null!) };

        // Act
        var result = await converter.ConvertAsync(element, context);

        // Assert
        Assert.NotNull(result);
        var resultList = Assert.IsType<List<string>>(result);
        Assert.Equal(3, resultList.Count);
        Assert.Equal("item_1", resultList[0]);
        Assert.Equal("item_2", resultList[1]);
        Assert.Equal("item_3", resultList[2]);
    }

    [Fact]
    public async Task TestLinkedItemCodenamesValueConverter_HandlesEmptyList()
    {
        // Arrange
        var converter = new TestLinkedItemCodenamesValueConverterAttribute();
        var element = new TestLinkedItemsElement([]);
        var context = new ResolvingContext { GetLinkedItem = _ => Task.FromResult<object>(null!) };

        // Act
        var result = await converter.ConvertAsync(element, context);

        // Assert
        Assert.NotNull(result);
        var resultList = Assert.IsType<List<string>>(result);
        Assert.Empty(resultList);
    }

    #endregion

    #region NodaTimeValueConverter Tests

    [Fact]
    public async Task NodaTimeValueConverter_ConvertsDateTimeToZonedDateTime()
    {
        // Arrange
        var converter = new NodaTimeValueConverterAttribute();
        var dateTime = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var element = new TestDateTimeElement(dateTime);
        var context = new ResolvingContext { GetLinkedItem = _ => Task.FromResult<object>(null!) };

        // Act
        var result = await converter.ConvertAsync(element, context);

        // Assert
        Assert.Equal(2024, result.Year);
        Assert.Equal(6, result.Month);
        Assert.Equal(15, result.Day);
        Assert.Equal(10, result.Hour);
        Assert.Equal(30, result.Minute);
    }

    [Fact]
    public async Task NodaTimeValueConverter_HandlesNullDateTime()
    {
        // Arrange
        var converter = new NodaTimeValueConverterAttribute();
        var element = new TestDateTimeElement(null);
        var context = new ResolvingContext { GetLinkedItem = _ => Task.FromResult<object>(null!) };

        // Act
        var result = await converter.ConvertAsync(element, context);

        // Assert
        Assert.Equal(default(ZonedDateTime), result);
    }

    [Fact]
    public async Task NodaTimeValueConverter_PreservesUtcTime()
    {
        // Arrange
        var converter = new NodaTimeValueConverterAttribute();
        var dateTime = new DateTime(2024, 12, 25, 0, 0, 0, DateTimeKind.Utc);
        var element = new TestDateTimeElement(dateTime);
        var context = new ResolvingContext { GetLinkedItem = _ => Task.FromResult<object>(null!) };

        // Act
        var result = await converter.ConvertAsync(element, context);

        // Assert
        Assert.Equal(DateTimeZone.Utc, result.Zone);
        Assert.Equal(12, result.Month);
        Assert.Equal(25, result.Day);
    }

    #endregion

    #region Test Element Implementations

    private sealed class TestStringElement(string? value) : IContentElementValue<string>
    {
        public string Value => value ?? string.Empty;
        public string Codename => "test_element";
        public string Name => "Test Element";
        public string Type => "text";
    }

    private sealed class TestLinkedItemsElement(List<string> codenames) : IContentElementValue<List<string>>
    {
        public List<string> Value => codenames;
        public string Codename => "linked_items";
        public string Name => "Linked Items";
        public string Type => "modular_content";
    }

    private sealed class TestDateTimeElement(DateTime? dateTime) : IContentElementValue<DateTime?>
    {
        public DateTime? Value => dateTime;
        public string Codename => "date_element";
        public string Name => "Date Element";
        public string Type => "date_time";
    }

    #endregion
}
