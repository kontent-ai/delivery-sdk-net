using System;
using FluentAssertions;
using Kontent.Ai.Delivery.Caching.Extensions;
using Xunit;

namespace Kontent.Ai.Delivery.Caching.Tests;

public class MessagePackSerializationTests
{
    private class Sample
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public DateTime When { get; set; }
    }

    [Fact]
    public void ToMessagePack_And_FromMessagePack_Roundtrips_Object()
    {
        var sample = new Sample { Name = "alpha", Count = 3, When = DateTime.UtcNow };    
        var bytes = sample.ToMessagePack();
        bytes.Should().NotBeNull();
        var roundtripped = bytes.FromMessagePack<Sample>();
        roundtripped.Should().NotBeNull();
        roundtripped.Should().BeEquivalentTo(sample);
    }

    [Fact]
    public void FromMessagePack_WithNullOrInvalid_ReturnsNull()
    {
        byte[] bytes = null;
        var result = bytes.FromMessagePack<Sample>();
        result.Should().BeNull();

        var invalid = new byte[] { 1, 2, 3, 4 };
        var invalidResult = invalid.FromMessagePack<Sample>();
        invalidResult.Should().BeNull();
    }
}
