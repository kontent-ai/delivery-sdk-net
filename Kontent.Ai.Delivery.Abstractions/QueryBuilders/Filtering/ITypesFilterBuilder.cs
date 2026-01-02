namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent filter builder for the content types endpoint.
/// Exposes only operators supported by the Types API.
/// </summary>
public interface ITypesFilterBuilder
{
    /// <summary>
    /// Selects a system property (e.g. <c>codename</c>, <c>name</c>, <c>last_modified</c>).
    /// </summary>
    ITypeFieldFilter<ITypesFilterBuilder> System(string propertyName);
}


