using System;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    public interface IRichTextElement : IContentElementValue<string>
    {
        IReadOnlyDictionary<Guid, IInlineImage> Images { get; }

        IReadOnlyDictionary<Guid, IContentLink> Links { get; }

        List<string> ModularContent { get; }
    }
}
