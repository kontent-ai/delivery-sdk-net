using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Kontent.Ai.Delivery.SourceGeneration.Tests;

public class ContentTypeGeneratorTests
{
    [Fact]
    public void Generator_WithValidContentTypes_GeneratesRegistry()
    {
        // Arrange
        var source = """
            using Kontent.Ai.Delivery.Attributes;

            namespace TestApp.Models;

            [ContentTypeCodename("article")]
            public class Article { }

            [ContentTypeCodename("home")]
            public record Home { }
            """;

        // Act
        var (diagnostics, output) = RunGenerator(source);

        // Assert
        diagnostics.Should().BeEmpty();
        output.Should().Contain("class ContentTypeRegistry : ITypeProvider");
        output.Should().Contain("\"article\"");
        output.Should().Contain("\"home\"");
        output.Should().Contain("typeof(global::TestApp.Models.Article)");
        output.Should().Contain("typeof(global::TestApp.Models.Home)");
    }

    [Fact]
    public void Generator_WithNoContentTypes_GeneratesEmptyRegistry()
    {
        // Arrange
        var source = """
            namespace TestApp.Models;

            public class Article { }
            """;

        // Act
        var (diagnostics, output) = RunGenerator(source);

        // Assert
        diagnostics.Should().BeEmpty();
        output.Should().Contain("class ContentTypeRegistry : ITypeProvider");
        output.Should().Contain("private static readonly Dictionary<string, Type> _codenameToType =");
    }

    [Fact]
    public void Generator_WithDuplicateCodename_ReportsError()
    {
        // Arrange
        var source = """
            using Kontent.Ai.Delivery.Attributes;

            namespace TestApp.Models;

            [ContentTypeCodename("article")]
            public class Article { }

            [ContentTypeCodename("article")]
            public class ArticleDuplicate { }
            """;

        // Act
        var (diagnostics, _) = RunGenerator(source);

        // Assert
        diagnostics.Any(d => d.Id == "KDSG001").Should().BeTrue();
        diagnostics.Any(d => d.GetMessage().Contains("Duplicate codename 'article'")).Should().BeTrue();
    }

    [Fact]
    public void Generator_WithEmptyCodename_ReportsError()
    {
        // Arrange
        var source = """
            using Kontent.Ai.Delivery.Attributes;

            namespace TestApp.Models;

            [ContentTypeCodename("")]
            public class Article { }
            """;

        // Act
        var (diagnostics, _) = RunGenerator(source);

        // Assert
        diagnostics.Any(d => d.Id == "KDSG002").Should().BeTrue();
    }

    [Fact]
    public void Generator_WithWhitespaceCodename_ReportsError()
    {
        // Arrange
        var source = """
            using Kontent.Ai.Delivery.Attributes;

            namespace TestApp.Models;

            [ContentTypeCodename("   ")]
            public class Article { }
            """;

        // Act
        var (diagnostics, _) = RunGenerator(source);

        // Assert
        diagnostics.Any(d => d.Id == "KDSG002").Should().BeTrue();
    }

    [Fact]
    public void Generator_WithInterface_ReportsError()
    {
        // Arrange
        var source = """
            using Kontent.Ai.Delivery.Attributes;

            namespace TestApp.Models;

            [ContentTypeCodename("article")]
            public interface IArticle { }
            """;

        // Act
        var (diagnostics, _) = RunGenerator(source);

        // Assert
        diagnostics.Any(d => d.Id == "KDSG003").Should().BeTrue();
        diagnostics.Any(d => d.GetMessage().Contains("IArticle")).Should().BeTrue();
    }

    [Fact]
    public void Generator_WithAbstractClass_ReportsError()
    {
        // Arrange
        var source = """
            using Kontent.Ai.Delivery.Attributes;

            namespace TestApp.Models;

            [ContentTypeCodename("article")]
            public abstract class Article { }
            """;

        // Act
        var (diagnostics, _) = RunGenerator(source);

        // Assert
        diagnostics.Any(d => d.Id == "KDSG003").Should().BeTrue();
        diagnostics.Any(d => d.GetMessage().Contains("Article")).Should().BeTrue();
    }

    [Fact]
    public void Generator_WithStruct_GeneratesRegistry()
    {
        // Arrange
        var source = """
            using Kontent.Ai.Delivery.Attributes;

            namespace TestApp.Models;

            [ContentTypeCodename("article")]
            public struct Article { }
            """;

        // Act
        var (diagnostics, output) = RunGenerator(source);

        // Assert
        diagnostics.Should().BeEmpty();
        output.Should().Contain("typeof(global::TestApp.Models.Article)");
    }

    [Fact]
    public void Generator_CaseInsensitiveCodenameComparison_DetectsDuplicates()
    {
        // Arrange
        var source = """
            using Kontent.Ai.Delivery.Attributes;

            namespace TestApp.Models;

            [ContentTypeCodename("Article")]
            public class Article { }

            [ContentTypeCodename("ARTICLE")]
            public class ArticleUpperCase { }
            """;

        // Act
        var (diagnostics, _) = RunGenerator(source);

        // Assert
        diagnostics.Any(d => d.Id == "KDSG001").Should().BeTrue();
    }

    [Fact]
    public void Generator_OrdersEntriesAlphabetically()
    {
        // Arrange
        var source = """
            using Kontent.Ai.Delivery.Attributes;

            namespace TestApp.Models;

            [ContentTypeCodename("zebra")]
            public class Zebra { }

            [ContentTypeCodename("apple")]
            public class Apple { }

            [ContentTypeCodename("mango")]
            public class Mango { }
            """;

        // Act
        var (diagnostics, output) = RunGenerator(source);

        // Assert
        diagnostics.Should().BeEmpty();
        var appleIndex = output.IndexOf("\"apple\"");
        var mangoIndex = output.IndexOf("\"mango\"");
        var zebraIndex = output.IndexOf("\"zebra\"");

        appleIndex.Should().BeLessThan(mangoIndex);
        mangoIndex.Should().BeLessThan(zebraIndex);
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, string Output) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        // Add reference to the Attributes assembly
        references.Add(MetadataReference.CreateFromFile(typeof(Attributes.ContentTypeCodenameAttribute).Assembly.Location));

        // Add reference to Abstractions assembly for ITypeProvider
        references.Add(MetadataReference.CreateFromFile(typeof(Abstractions.ITypeProvider).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ContentTypeGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedSource = runResult.GeneratedTrees.FirstOrDefault()?.GetText().ToString() ?? string.Empty;

        return (diagnostics, generatedSource);
    }
}
