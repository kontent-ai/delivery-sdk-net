using System;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    public interface IRichTextElement : IContentElementValue<string>
    {
        IDictionary<Guid, IInlineImage> Images { get; }

        IDictionary<Guid, IContentLink> Links { get; }

        List<string> ModularContent { get; }
    }
}
