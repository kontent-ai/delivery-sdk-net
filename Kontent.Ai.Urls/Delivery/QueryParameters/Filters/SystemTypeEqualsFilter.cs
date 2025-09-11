namespace Kontent.Ai.Urls.Delivery.QueryParameters.Filters;

/// <summary>
/// Represents a filter that matches a content item of the given content type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SystemTypeEqualsFilter"/> class.
/// </remarks>
/// <param name="codename">Content type codename.</param>
public sealed class SystemTypeEqualsFilter(string codename) : Filter("system.type", codename)
{
}
