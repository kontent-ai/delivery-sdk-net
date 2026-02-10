using System.Reflection;

namespace Kontent.Ai.Delivery.Extensions;

/// <summary>
/// Copies values between <see cref="DeliveryOptions"/> instances.
/// </summary>
internal static class DeliveryOptionsCopyHelper
{
    private static readonly PropertyInfo[] WritableProperties = [.. typeof(DeliveryOptions)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property is { CanRead: true, CanWrite: true } && property.GetIndexParameters().Length == 0)];

    public static void Copy(DeliveryOptions source, DeliveryOptions destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        foreach (var property in WritableProperties)
        {
            property.SetValue(destination, property.GetValue(source));
        }
    }
}
