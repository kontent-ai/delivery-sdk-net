using Kontent.Ai.Delivery.SharedModels;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.SharedModels;

public class TaxonomyTermTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var sut = new TaxonomyTerm
        {
            Name = "Coffee",
            Codename = "coffee"
        };

        Assert.Equal("Coffee", sut.Name);
        Assert.Equal("coffee", sut.Codename);
    }

    [Fact]
    public void WithExpression_CreatesClone()
    {
        var sut = new TaxonomyTerm
        {
            Name = "Coffee",
            Codename = "coffee"
        };

        var clone = sut with { };

        Assert.NotSame(sut, clone);
        Assert.Equal(sut.Name, clone.Name);
        Assert.Equal(sut.Codename, clone.Codename);
    }
}
