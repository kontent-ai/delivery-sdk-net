using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using RichardSzalay.MockHttp;

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
}
