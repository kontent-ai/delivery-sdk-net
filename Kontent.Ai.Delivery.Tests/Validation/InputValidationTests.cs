using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Validation;

/// <summary>
/// Tests for input validation in the DeliveryClient.
/// Verifies that invalid inputs are properly validated and rejected.
/// </summary>
public sealed class InputValidationTests
{
    private readonly Guid _guid = Guid.NewGuid();

    private IDeliveryClient CreateClient()
    {
        var services = new ServiceCollection();
        var mockHttp = new MockHttpMessageHandler();
        var options = new DeliveryOptions { EnvironmentId = _guid.ToString() };
        services.AddDeliveryClient(options, configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    #region GetItem Codename Validation

    [Fact]
    public void GetItem_EmptyCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetItem<IDynamicElements>(string.Empty));
        Assert.Equal("codename", exception.ParamName);
        Assert.Contains("not valid", exception.Message);
    }

    [Fact]
    public void GetItem_NullCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetItem<IDynamicElements>(null!));
        Assert.Equal("codename", exception.ParamName);
    }

    [Fact]
    public void GetItemDynamic_EmptyCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetItem(string.Empty));
        Assert.Equal("codename", exception.ParamName);
        Assert.Contains("not valid", exception.Message);
    }

    [Fact]
    public void GetItemDynamic_NullCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetItem(null!));
        Assert.Equal("codename", exception.ParamName);
    }

    #endregion

    #region GetType Codename Validation

    [Fact]
    public void GetType_EmptyCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetType(string.Empty));
        Assert.Equal("codename", exception.ParamName);
        Assert.Contains("not valid", exception.Message);
    }

    [Fact]
    public void GetType_NullCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetType(null!));
        Assert.Equal("codename", exception.ParamName);
    }

    #endregion

    #region GetTaxonomy Codename Validation

    [Fact]
    public void GetTaxonomy_EmptyCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetTaxonomy(string.Empty));
        Assert.Equal("codename", exception.ParamName);
        Assert.Contains("not valid", exception.Message);
    }

    [Fact]
    public void GetTaxonomy_NullCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetTaxonomy(null!));
        Assert.Equal("codename", exception.ParamName);
    }

    #endregion

    #region GetContentElement Codename Validation

    [Fact]
    public void GetContentElement_EmptyContentTypeCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetContentElement(string.Empty, "title"));
        Assert.Equal("contentTypeCodename", exception.ParamName);
        Assert.Contains("not valid", exception.Message);
    }

    [Fact]
    public void GetContentElement_EmptyElementCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetContentElement("article", string.Empty));
        Assert.Equal("contentElementCodename", exception.ParamName);
        Assert.Contains("not valid", exception.Message);
    }

    [Fact]
    public void GetContentElement_NullContentTypeCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetContentElement(null!, "title"));
        Assert.Equal("contentTypeCodename", exception.ParamName);
    }

    [Fact]
    public void GetContentElement_NullElementCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetContentElement("article", null!));
        Assert.Equal("contentElementCodename", exception.ParamName);
    }

    #endregion

    #region GetItemUsedIn Codename Validation

    [Fact]
    public void GetItemUsedIn_EmptyCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetItemUsedIn(string.Empty));
        Assert.Equal("codename", exception.ParamName);
        Assert.Contains("not valid", exception.Message);
    }

    [Fact]
    public void GetItemUsedIn_NullCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetItemUsedIn(null!));
        Assert.Equal("codename", exception.ParamName);
    }

    #endregion

    #region GetAssetUsedIn Codename Validation

    [Fact]
    public void GetAssetUsedIn_EmptyCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetAssetUsedIn(string.Empty));
        Assert.Equal("codename", exception.ParamName);
        Assert.Contains("not valid", exception.Message);
    }

    [Fact]
    public void GetAssetUsedIn_NullCodename_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() => client.GetAssetUsedIn(null!));
        Assert.Equal("codename", exception.ParamName);
    }

    #endregion

    #region Filter Validation

    [Fact]
    public void Filter_EmptyPropertyName_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() =>
            client.GetItems<IDynamicElements>()
                .Where(f => f.Element(string.Empty).IsEqualTo("value")));

        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void Filter_PropertyNameWithSpaces_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() =>
            client.GetItems<IDynamicElements>()
                .Where(f => f.Element("invalid name").IsEqualTo("value")));

        Assert.Contains("contains spaces", exception.Message);
    }

    [Fact]
    public void Filter_IsInWithEmptyArray_ThrowsArgumentException()
    {
        var client = CreateClient();
        var emptyArray = Array.Empty<string>();

        var exception = Assert.Throws<ArgumentException>(() =>
            client.GetItems<IDynamicElements>()
                .Where(f => f.Element("field").IsIn(emptyArray)));

        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public void Filter_ContainsAnyWithEmptyArray_ThrowsArgumentException()
    {
        var client = CreateClient();
        var emptyArray = Array.Empty<string>();

        var exception = Assert.Throws<ArgumentException>(() =>
            client.GetItems<IDynamicElements>()
                .Where(f => f.Element("tags").ContainsAny(emptyArray)));

        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public void Filter_RangeWithInvalidBounds_ThrowsArgumentException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<ArgumentException>(() =>
            client.GetItems<IDynamicElements>()
                .Where(f => f.Element("price").IsWithinRange(100.0, 50.0)));

        Assert.Contains("Invalid range", exception.Message);
    }

    [Fact]
    public void Filter_DateRangeWithInvalidBounds_ThrowsArgumentException()
    {
        var client = CreateClient();
        var laterDate = DateTime.UtcNow;
        var earlierDate = laterDate.AddDays(-10);

        var exception = Assert.Throws<ArgumentException>(() =>
            client.GetItems<IDynamicElements>()
                .Where(f => f.Element("publish_date").IsWithinRange(laterDate, earlierDate)));

        Assert.Contains("Invalid date range", exception.Message);
    }

    #endregion
}
