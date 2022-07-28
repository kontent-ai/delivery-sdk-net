using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Kontent.Ai.Delivery.Abstractions.Tests;

public class CheckNamespaces
{
    private readonly ITestOutputHelper output;

    public CheckNamespaces(ITestOutputHelper output)
    {
        this.output = output;
    }

    /// <summary>
    /// See Kontent.Ai.Delivery.Abstractions Readme for more information.
    /// </summary>
    [Fact]
    public void AllNamespacecAreCorrect()
    {
        var abstractionTypes = Assembly.LoadFrom("Kontent.Ai.Delivery.Abstractions.dll");

        Assert.All(abstractionTypes.GetTypes(), t => Assert.Equal("Kontent.Ai.Delivery.Abstractions", t.Namespace));
    }
}