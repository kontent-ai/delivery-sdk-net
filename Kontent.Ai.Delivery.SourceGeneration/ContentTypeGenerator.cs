using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Kontent.Ai.Delivery.SourceGeneration;

/// <summary>
/// Incremental source generator that creates a compile-time type registry
/// from classes decorated with [ContentType].
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ContentTypeGenerator : IIncrementalGenerator
{
    private const string ContentTypeCodenameAttributeFullName = "Kontent.Ai.Delivery.Attributes.ContentTypeCodenameAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Stage 1: Syntax filter - find all type declarations with attributes
        var typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateTypeDeclaration(node),
                transform: static (ctx, ct) => GetContentTypeInfo(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value);

        // Stage 2: Collect all content type infos and generate
        var collected = typeDeclarations.Collect();

        context.RegisterSourceOutput(collected, static (spc, infos) => Execute(spc, infos));
    }

    private static bool IsCandidateTypeDeclaration(SyntaxNode node)
    {
        // Only interested in class, struct, or record declarations with attributes
        return node is TypeDeclarationSyntax typeDecl
            && typeDecl.AttributeLists.Count > 0;
    }

    private static ContentTypeInfo? GetContentTypeInfo(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var typeDecl = (TypeDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Get the symbol for the type declaration
        if (semanticModel.GetDeclaredSymbol(typeDecl, ct) is not INamedTypeSymbol typeSymbol)
            return null;

        // Find the ContentTypeCodenameAttribute
        foreach (var attributeData in typeSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() != ContentTypeCodenameAttributeFullName)
                continue;

            // Found the attribute - extract the codename
            string? codename = null;
            if (attributeData.ConstructorArguments.Length > 0)
            {
                var arg = attributeData.ConstructorArguments[0];
                codename = arg.Value as string;
            }

            // Get location for diagnostics
            var location = attributeData.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation()
                ?? typeDecl.Identifier.GetLocation();

            return new ContentTypeInfo(
                codename: codename,
                fullyQualifiedTypeName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                typeName: typeSymbol.Name,
                location: location,
                isInterface: typeSymbol.TypeKind == TypeKind.Interface,
                isAbstract: typeSymbol.IsAbstract && typeSymbol.TypeKind == TypeKind.Class);
        }

        return null;
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<ContentTypeInfo> infos)
    {
        // Group by codename to detect duplicates
        var validInfos = new List<ContentTypeInfo>();
        var codenameToTypes = new Dictionary<string, List<ContentTypeInfo>>(StringComparer.OrdinalIgnoreCase);

        foreach (var info in infos)
        {
            // Check for invalid codename
            if (string.IsNullOrWhiteSpace(info.Codename))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidCodename,
                    info.Location));
                continue;
            }

            // Check for unsupported target types
            if (info.IsInterface || info.IsAbstract)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedTargetType,
                    info.Location,
                    info.TypeName));
                continue;
            }

            // Track for duplicate detection
            if (!codenameToTypes.TryGetValue(info.Codename!, out var typeList))
            {
                typeList = new List<ContentTypeInfo>();
                codenameToTypes[info.Codename!] = typeList;
            }
            typeList.Add(info);
            validInfos.Add(info);
        }

        // Report duplicate codenames
        var duplicateCodenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in codenameToTypes)
        {
            if (kvp.Value.Count > 1)
            {
                duplicateCodenames.Add(kvp.Key);
                var typeNames = string.Join(", ", kvp.Value.Select(i => i.TypeName));
                foreach (var info in kvp.Value)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateCodename,
                        info.Location,
                        kvp.Key,
                        typeNames));
                }
            }
        }

        // Filter out types with duplicate codenames from generation
        var finalInfos = validInfos
            .Where(i => !duplicateCodenames.Contains(i.Codename!))
            .OrderBy(i => i.Codename, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Always generate the registry (even if empty)
        var source = GenerateRegistry(finalInfos);
        context.AddSource("ContentTypeRegistry.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateRegistry(List<ContentTypeInfo> infos)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Kontent.Ai.Delivery.Abstractions;");
        sb.AppendLine();
        sb.AppendLine("namespace Kontent.Ai.Delivery.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated content type registry mapping codenames to CLR types.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public sealed class ContentTypeRegistry : ITypeProvider");
        sb.AppendLine("{");

        // Codename to Type dictionary
        sb.AppendLine("    private static readonly Dictionary<string, Type> _codenameToType =");
        sb.AppendLine("        new(StringComparer.OrdinalIgnoreCase)");
        sb.AppendLine("    {");
        foreach (var info in infos)
        {
            sb.AppendLine($"        {{ \"{EscapeString(info.Codename!)}\", typeof({info.FullyQualifiedTypeName}) }},");
        }
        sb.AppendLine("    };");
        sb.AppendLine();

        // Type to Codename dictionary
        sb.AppendLine("    private static readonly Dictionary<Type, string> _typeToCodename = new()");
        sb.AppendLine("    {");
        foreach (var info in infos)
        {
            sb.AppendLine($"        {{ typeof({info.FullyQualifiedTypeName}), \"{EscapeString(info.Codename!)}\" }},");
        }
        sb.AppendLine("    };");
        sb.AppendLine();

        // GetType method
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public Type? GetType(string contentType)");
        sb.AppendLine("        => _codenameToType.TryGetValue(contentType, out var type) ? type : null;");
        sb.AppendLine();

        // GetCodename method
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public string? GetCodename(Type contentType)");
        sb.AppendLine("        => _typeToCodename.TryGetValue(contentType, out var codename) ? codename : null;");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private readonly struct ContentTypeInfo
    {
        public string? Codename { get; }
        public string FullyQualifiedTypeName { get; }
        public string TypeName { get; }
        public Location Location { get; }
        public bool IsInterface { get; }
        public bool IsAbstract { get; }

        public ContentTypeInfo(
            string? codename,
            string fullyQualifiedTypeName,
            string typeName,
            Location location,
            bool isInterface,
            bool isAbstract)
        {
            Codename = codename;
            FullyQualifiedTypeName = fullyQualifiedTypeName;
            TypeName = typeName;
            Location = location;
            IsInterface = isInterface;
            IsAbstract = isAbstract;
        }
    }
}
