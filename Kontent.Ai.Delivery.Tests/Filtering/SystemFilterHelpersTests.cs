using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class SystemFilterHelpersTests
{
    [Fact]
    public void SystemFilterHelpers_AddSystemLanguageFilter_AddsSystemLanguageEq()
    {
        var filters = new SerializedFilterCollection();

        SystemFilterHelpers.AddSystemLanguageFilter(filters, "es-ES");

        Assert.Contains(new KeyValuePair<string, string>("system.language[eq]", "es-ES"), filters);
    }

    [Fact]
    public void SystemFilterHelpers_AddGenericTypeFilter_RespectsDynamicModelAndMissingCodename()
    {
        var filters = new SerializedFilterCollection();
        var missingCodenameProvider = new StubTypeProvider(codename: null);
        var resolvedCodenameProvider = new StubTypeProvider(codename: "article");

        // Dynamic models should never get automatic type filters.
        SystemFilterHelpers.AddGenericTypeFilter<IDynamicElements>(filters, resolvedCodenameProvider, logger: null);
        Assert.Empty(filters);

        // Strongly-typed models with unknown codename should not add filters.
        SystemFilterHelpers.AddGenericTypeFilter<TestModel>(filters, missingCodenameProvider, logger: null);
        Assert.Empty(filters);
    }

    private sealed class TestModel;

    private sealed class StubTypeProvider(string? codename) : ITypeProvider
    {
        public Type? GetType(string contentType) => null;

        public string? GetCodename(Type contentType) => codename;
    }
}
