namespace Kontent.Ai.Delivery.Api.Filtering;

internal static class FilterPath
{
    internal static string System(string propertyName) => Build("system", propertyName);
    internal static string Element(string elementCodename) => Build("elements", elementCodename);

    private static string Build(string prefix, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Property name cannot be null or whitespace.", nameof(name));
        }

        var trimmed = name.Trim();

        if (trimmed.Contains(' '))
        {
            throw new ArgumentException($"Property name '{name}' contains spaces.", nameof(name));
        }

        var expectedPrefix = prefix + ".";
        if (trimmed.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        // Avoid accepting dotted input with the wrong prefix (e.g. System("elements.title")).
        return trimmed.Contains('.', StringComparison.Ordinal)
            ? throw new ArgumentException(
                $"Property name '{name}' must be provided without a prefix. Use '{prefix}.' prefix only.",
                nameof(name))
            : $"{prefix}.{trimmed}";
    }
}
