using System.Reflection;

namespace Kontent.Ai.Delivery.Abstractions.Tests.Configuration;

public class DeliveryOptionsCopyToTests
{
    [Fact]
    public void CopyTo_CopiesEveryPublicWritableProperty()
    {
        var writableProperties = typeof(DeliveryOptions)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p is { CanRead: true, CanWrite: true } && p.GetIndexParameters().Length == 0)
            .ToArray();

        Assert.NotEmpty(writableProperties);

        var defaults = new DeliveryOptions();
        var source = new DeliveryOptions();
        foreach (var property in writableProperties)
        {
            property.SetValue(source, DistinctValueFor(property, property.GetValue(defaults)));
        }

        var destination = new DeliveryOptions();
        source.CopyTo(destination);

        foreach (var property in writableProperties)
        {
            var expected = property.GetValue(source);
            var actual = property.GetValue(destination);
            Assert.True(
                Equals(expected, actual),
                $"DeliveryOptions.CopyTo did not copy '{property.Name}'. " +
                $"Expected '{expected}' but got '{actual}'. " +
                "Did you add a new property without updating CopyTo?");
        }
    }

    private static object DistinctValueFor(PropertyInfo property, object? defaultValue)
    {
        if (property.PropertyType == typeof(bool))
        {
            return !(bool)(defaultValue ?? false);
        }

        if (property.PropertyType == typeof(string))
        {
            return $"copyto-{property.Name}";
        }

        throw new NotSupportedException(
            $"Add a distinct-value generator for '{property.Name}' of type '{property.PropertyType}' to this guard test.");
    }
}
