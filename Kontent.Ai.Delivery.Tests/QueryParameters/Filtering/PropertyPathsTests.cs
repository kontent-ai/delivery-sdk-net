using System;
using Kontent.Ai.Delivery.Abstractions;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryParameters.Filtering;

public class PropertyPathsTests
{
    #region ItemSystemPath Tests

    [Fact]
    public void ItemSystemPath_Serialize_ReturnsCorrectPath()
    {
        Assert.Equal("system.type", ItemSystemPath.Type.Serialize());
        Assert.Equal("system.last_modified", ItemSystemPath.LastModified.Serialize());
        Assert.Equal("system.workflow_step", ItemSystemPath.WorkflowStep.Serialize());
    }

    [Fact]
    public void ItemSystemPath_ImplicitStringConversion_Works()
    {
        string path = ItemSystemPath.Type;
        Assert.Equal("system.type", path);
    }

    [Fact]
    public void ItemSystemPath_ToString_ReturnsCorrectValue()
    {
        Assert.Equal("system.collection", ItemSystemPath.Collection.ToString());
    }

    #endregion

    #region TypeSystemPath Tests

    [Fact]
    public void TypeSystemPath_Serialize_ReturnsCorrectPath()
    {
        Assert.Equal("system.codename", TypeSystemPath.Codename.Serialize());
        Assert.Equal("system.last_modified", TypeSystemPath.LastModified.Serialize());
    }

    [Fact]
    public void TypeSystemPath_ImplicitStringConversion_Works()
    {
        string path = TypeSystemPath.Name;
        Assert.Equal("system.name", path);
    }

    [Fact]
    public void TypeSystemPath_ToString_ReturnsCorrectValue()
    {
        Assert.Equal("system.id", TypeSystemPath.Id.ToString());
    }

    #endregion

    #region TaxonomySystemPath Tests

    [Fact]
    public void TaxonomySystemPath_Serialize_ReturnsCorrectPath()
    {
        Assert.Equal("system.codename", TaxonomySystemPath.Codename.Serialize());
    }

    [Fact]
    public void TaxonomySystemPath_ImplicitStringConversion_Works()
    {
        string path = TaxonomySystemPath.LastModified;
        Assert.Equal("system.last_modified", path);
    }

    [Fact]
    public void TaxonomySystemPath_ToString_ReturnsCorrectValue()
    {
        Assert.Equal("system.name", TaxonomySystemPath.Name.ToString());
    }

    #endregion

    #region ElementPath Tests

    [Fact]
    public void ElementPath_Serialize_ReturnsCorrectPath()
    {
        var path = Elements.GetPath("title");
        Assert.Equal("elements.title", path.Serialize());
    }

    [Fact]
    public void ElementPath_Codename_ReturnsJustCodename()
    {
        var path = Elements.GetPath("author_name");
        Assert.Equal("author_name", path.Codename);
        Assert.Equal("elements.author_name", path.Serialize());
    }

    [Fact]
    public void ElementPath_ImplicitStringConversion_Works()
    {
        string path = Elements.GetPath("price");
        Assert.Equal("elements.price", path);
    }

    [Fact]
    public void ElementPath_ToString_ReturnsCorrectValue()
    {
        var path = Elements.GetPath("description");
        Assert.Equal("elements.description", path.ToString());
    }

    [Fact]
    public void ElementPath_ComplexCodename_Works()
    {
        var path = Elements.GetPath("my_complex_element_name");
        Assert.Equal("my_complex_element_name", path.Codename);
        Assert.Equal("elements.my_complex_element_name", path.Serialize());
    }

    #endregion

    #region ElementPath Validation Tests

    [Fact]
    public void ElementPath_ThrowsOnNull()
    {
        var ex = Assert.Throws<ArgumentException>(() => Elements.GetPath(null!));
        Assert.Contains("cannot be null or whitespace", ex.Message);
    }

    [Fact]
    public void ElementPath_ThrowsOnEmpty()
    {
        var ex = Assert.Throws<ArgumentException>(() => Elements.GetPath(""));
        Assert.Contains("cannot be null or whitespace", ex.Message);
    }

    [Fact]
    public void ElementPath_ThrowsOnWhitespace()
    {
        var ex = Assert.Throws<ArgumentException>(() => Elements.GetPath("   "));
        Assert.Contains("cannot be null or whitespace", ex.Message);
    }

    [Fact]
    public void ElementPath_ThrowsOnCodenameWithSpaces()
    {
        var ex = Assert.Throws<ArgumentException>(() => Elements.GetPath("my element"));
        Assert.Contains("contains spaces", ex.Message);
        Assert.Contains("my element", ex.Message);
    }

    [Fact]
    public void ElementPath_AllowsUnderscores()
    {
        var path = Elements.GetPath("author_name");
        Assert.Equal("elements.author_name", path.Serialize());
    }

    [Fact]
    public void ElementPath_AllowsHyphens()
    {
        var path = Elements.GetPath("author-name");
        Assert.Equal("elements.author-name", path.Serialize());
    }

    [Fact]
    public void ElementPath_AllowsNumbers()
    {
        var path = Elements.GetPath("field123");
        Assert.Equal("elements.field123", path.Serialize());
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void ItemSystemPath_Equality_Works()
    {
        var path1 = ItemSystemPath.Type;
        var path2 = ItemSystemPath.Type;
        Assert.Equal(path1, path2);
    }

    [Fact]
    public void ElementPath_Equality_Works()
    {
        var path1 = Elements.GetPath("title");
        var path2 = Elements.GetPath("title");
        Assert.Equal(path1, path2);
    }

    [Fact]
    public void ElementPath_Inequality_Works()
    {
        var path1 = Elements.GetPath("title");
        var path2 = Elements.GetPath("author");
        Assert.NotEqual(path1, path2);
    }

    #endregion
}