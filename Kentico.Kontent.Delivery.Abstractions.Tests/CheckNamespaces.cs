using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Kentico.Kontent.Delivery.Abstractions.Tests;

public class CheckNamespaces
{
    private readonly ITestOutputHelper output;

    public CheckNamespaces(ITestOutputHelper output)
    {
        this.output = output;
    }

    /// <summary>
    /// See Kentico.Kontent.Delivery.Abstractions Readme for more information.
    /// </summary>
    [Fact]
    public void AllNamespacecAreCorrect()
    {
        var abstractionTypes = Assembly.LoadFrom("Kentico.Kontent.Delivery.Abstractions.dll");

        Assert.All(abstractionTypes.GetTypes(), t => Assert.Equal("Kentico.Kontent.Delivery.Abstractions", t.Namespace));
    }
}