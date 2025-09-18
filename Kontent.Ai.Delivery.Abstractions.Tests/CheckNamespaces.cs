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

        var typesToCheck = abstractionTypes
            .GetTypes()
            // Exclude compiler-generated artifacts and synthesized types
            .Where(t => t.GetCustomAttribute(typeof(CompilerGeneratedAttribute), inherit: true) == null)
            .Where(t => !t.Name.StartsWith("<>"))
            // Only consider types that actually have a namespace
            .Where(t => t.Namespace is not null);

        Assert.All(
            typesToCheck,
            t => Assert.StartsWith("Kontent.Ai.Delivery.Abstractions", t.Namespace));
    }
}