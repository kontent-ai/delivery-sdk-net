Sometimes, you want to use your custom types in your models and let the `IDeliveryClient` to deserialize their values. This can be achieved by using so-called "value converters".

You simply decorate properties of models with an attribute implementing the `Kentico.Kontent.Delivery.Abstractions.IPropertyValueConverter<T>` interface.

**Model:**

```csharp
[NodaTimeValueConverter]
public ZonedDateTime PostDate { get; set; }
```

**Converter:**

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class NodaTimeValueConverterAttribute : Attribute, IPropertyValueConverter<string>
{
	public Task<object> GetPropertyValueAsync<TElement>(PropertyInfo property, TElement element, ResolvingContext context) where TElement : IContentElementValue<DateTime>
	{
		var udt = DateTime.SpecifyKind(element.Value, DateTimeKind.Utc);
		return Task.FromResult((object)ZonedDateTime.FromDateTimeOffset(udt));
	}
}
```

**Usage:**

```csharp
var item = await client.GetItemAsync<YourModelType>("codename");
ZonedDateTime dt = item.Item.PostDateNodaTime; // Your custom-typed property
```

See a sample unit test: https://github.com/Kentico/kontent-delivery-sdk-net/blob/master/Kentico.Kontent.Delivery.Tests/ValueConverterTests.cs
