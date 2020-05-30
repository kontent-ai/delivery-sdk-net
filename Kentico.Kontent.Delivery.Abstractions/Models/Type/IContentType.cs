using Kentico.Kontent.Delivery.Abstractions.Models.Type.Element;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions.Models.Type
{
    public interface IContentType
    {
        IReadOnlyDictionary<string, IContentElement> Elements { get; }
        IContentTypeSystemAttributes System { get; }
    }
}