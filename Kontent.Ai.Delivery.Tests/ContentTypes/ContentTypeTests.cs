using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentTypes;
using Kontent.Ai.Delivery.ContentTypes.Element;
using Kontent.Ai.Delivery.SharedModels;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ContentTypes;

public class ContentTypeTests
{
    [Fact]
    public void ExplicitInterface_Elements_ReturnsDictionaryOfIContentElement()
    {
        var element = new ContentElement { Type = "text", Name = "Title", Codename = "title" };
        var sut = new ContentType
        {
            System = new ContentTypeSystemAttributes
            {
                Id = Guid.NewGuid(),
                Name = "Article",
                Codename = "article",
                LastModified = DateTime.UtcNow
            },
            Elements = new Dictionary<string, ContentElement> { ["title"] = element }
        };

        IContentType iface = sut;
        var elements = iface.Elements;

        Assert.Single(elements);
        Assert.True(elements.ContainsKey("title"));
        Assert.Equal("Title", elements["title"].Name);
    }

    [Fact]
    public void ExplicitInterface_MultipleChoiceElement_Options_ReturnsList()
    {
        var option = new MultipleChoiceOption { Name = "Yes", Codename = "yes" };
        var sut = new MultipleChoiceElement
        {
            Type = "multiple_choice",
            Name = "Featured",
            Codename = "featured",
            Options = [option]
        };

        IMultipleChoiceElement iface = sut;

        Assert.Single(iface.Options);
        Assert.Equal("Yes", iface.Options[0].Name);
    }
}
