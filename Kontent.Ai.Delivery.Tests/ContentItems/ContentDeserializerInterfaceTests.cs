using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ContentItems;

public sealed class ContentDeserializerInterfaceTests
{
    [Fact]
    public void DeserializeContentItem_JsonElement_UsesDefaultInterfaceImplementation()
    {
        IContentDeserializer sut = new SpyDeserializer();
        var jsonElement = JsonDocument.Parse("""{"system":{"type":"article"},"elements":{}}""").RootElement;

        var result = sut.DeserializeContentItem(jsonElement, typeof(object));

        var spy = Assert.IsType<SpyDeserializer>(sut);
        Assert.Equal(jsonElement.GetRawText(), spy.CapturedJson);
        Assert.Equal(typeof(object), spy.CapturedModelType);
        Assert.Equal("ok", result);
    }

    private sealed class SpyDeserializer : IContentDeserializer
    {
        public string? CapturedJson { get; private set; }
        public Type? CapturedModelType { get; private set; }

        public object DeserializeContentItem(string json, Type modelType)
        {
            CapturedJson = json;
            CapturedModelType = modelType;
            return "ok";
        }
    }
}
