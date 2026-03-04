using System.Reflection;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Generated;

namespace Kontent.Ai.Delivery.Tests.ContentItems;

public class TypeProviderTests
{
    // TypeProvider relies on static Lazy<> assembly discovery.
    // In the test runner context, the GeneratedTypeProvider is not discovered
    // (entry assembly is the test runner, not the app), so both methods return null.

    [Fact]
    public void GetType_UnknownCodename_ReturnsNull()
    {
        var sut = new TypeProvider();

        var result = sut.GetType("nonexistent_type");

        Assert.Null(result);
    }

    [Fact]
    public void GetCodename_UnknownType_ReturnsNull()
    {
        var sut = new TypeProvider();

        var result = sut.GetCodename(typeof(UnknownTestModel));

        Assert.Null(result);
    }

    [Fact]
    public void TryCreateProviderFromAssembly_WhenGeneratedProviderExists_ReturnsInstance()
    {
        var method = typeof(TypeProvider).GetMethod("TryCreateProviderFromAssembly", BindingFlags.NonPublic | BindingFlags.Static)!;
        var assembly = typeof(GeneratedTypeProvider).Assembly;

        var result = method.Invoke(null, [assembly]);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<ITypeProvider>(result);
    }

    [Fact]
    public void TryLoadAssembly_WhenAssemblyCannotBeLoaded_ReturnsNull()
    {
        var method = typeof(TypeProvider).GetMethod("TryLoadAssembly", BindingFlags.NonPublic | BindingFlags.Static)!;
        var missingReference = new AssemblyName("Definitely.Missing.Assembly.For.TypeProvider.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

        var result = method.Invoke(null, [missingReference]);

        Assert.Null(result);
    }

    private sealed class UnknownTestModel;
}
