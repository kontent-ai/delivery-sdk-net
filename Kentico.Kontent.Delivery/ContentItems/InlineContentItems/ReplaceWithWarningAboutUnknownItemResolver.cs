﻿using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.InlineContentItems;

namespace Kentico.Kontent.Delivery.ContentItems.InlineContentItems
{
    internal class ReplaceWithWarningAboutUnknownItemResolver : IInlineContentItemsResolver<UnknownContentItem>
    {
        public string Resolve(UnknownContentItem item)
            => $"Content type '{item.Type}' has no corresponding model.";
    }
}
