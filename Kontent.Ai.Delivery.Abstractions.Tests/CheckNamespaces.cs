using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Kontent.Ai.Delivery.Abstractions;

public class CheckNamespaces(ITestOutputHelper output)
{
    private readonly ITestOutputHelper output = output;

    /// <summary>
    /// See Kontent.Ai.Delivery.Abstractions Readme for more information.
    /// </summary>
    [Fact]
    public void AllNamespacecAreCorrect()
    {
        var abstractionTypes = Assembly.LoadFrom("Kontent.Ai.Delivery.Abstractions.dll");

        Assert.All(
            abstractionTypes.GetTypes().Where(t => t.GetCustomAttribute(typeof(CompilerGeneratedAttribute), true) == null),
            t => Assert.Equal("Kontent.Ai.Delivery.Abstractions", t.Namespace));
    }
}