using Kontent.Ai.Delivery.Api.QueryParams.Types;

namespace Kontent.Ai.Delivery.Tests.QueryParameters;

public class SingleTypeParamsTests
{
    [Fact]
    public void Elements_DefaultsToNull()
    {
        var sut = new SingleTypeParams();

        Assert.Null(sut.Elements);
    }

    [Fact]
    public void WithExpression_CreatesCopyWithElements()
    {
        var sut = new SingleTypeParams { Elements = "title,summary" };

        var copy = sut with { };

        Assert.NotSame(sut, copy);
        Assert.Equal("title,summary", copy.Elements);
    }
}
