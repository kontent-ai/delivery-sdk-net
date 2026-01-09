using Xunit;

namespace Kontent.Ai.Delivery.Abstractions.Tests.ContentItems.Processing;

public class DependencyTrackingContextComponentTests
{
    [Theory]
    [InlineData("n27ec1626_93ac_0129_64e5_1beeda45416c")] // normalized id, 3rd group starts with 01
    [InlineData("N27ec1626_93ac_0129_64e5_1beeda45416c")]
    [InlineData("n27ec1626_93ac_01xx_64e5_1beeda45416c")] // begins with 01 => treated as component key
    [InlineData("n27ec1626_93ac_0129")] // begins with 01 => treated as component key (even if malformed/truncated)
    public void TrackItem_DoesNotRecordComponentKeys(string key)
    {
        var ctx = new DependencyTrackingContext();
        ctx.TrackItem(key);
        Assert.Empty(ctx.Dependencies);
    }

    [Theory]
    [InlineData("coffee_beverages_explained")]
    [InlineData("n27ec1626_93ac_4629_64e5_1beeda45416c")] // normalized id but 3rd group doesn't start with 01
    [InlineData("n27ec1626_93ac_012_64e5_1beeda45416c")] // 3rd group length != 4 => not a component key
    [InlineData("n27ec1626_93ac")] // missing 3rd group => not a component key
    public void TrackItem_RecordsContentItems(string key)
    {
        var ctx = new DependencyTrackingContext();
        ctx.TrackItem(key);
        Assert.Contains($"item_{key}", ctx.Dependencies);
    }
}

