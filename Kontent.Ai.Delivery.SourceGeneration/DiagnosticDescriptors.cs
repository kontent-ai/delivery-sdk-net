using Microsoft.CodeAnalysis;

namespace Kontent.Ai.Delivery.SourceGeneration;

/// <summary>
/// Diagnostic descriptors for the ContentType source generator.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "Kontent.Ai.Delivery.SourceGeneration";

    /// <summary>
    /// KDSG001: Duplicate codename used by multiple types.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateCodename = new(
        id: "KDSG001",
        title: "Duplicate content type codename",
        messageFormat: "Duplicate codename '{0}' used by: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each content type codename must be unique across all types decorated with [ContentTypeCodename].");

    /// <summary>
    /// KDSG002: Invalid codename - null, empty, or whitespace.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidCodename = new(
        id: "KDSG002",
        title: "Invalid content type codename",
        messageFormat: "Invalid codename: cannot be null, empty, or whitespace",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The content type codename must be a non-empty string.");

    /// <summary>
    /// KDSG003: Unsupported target type - interface or abstract class.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedTargetType = new(
        id: "KDSG003",
        title: "Unsupported target type for [ContentTypeCodename]",
        messageFormat: "[ContentTypeCodename] cannot be applied to interface or abstract class '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[ContentTypeCodename] can only be applied to concrete classes or structs.");
}
