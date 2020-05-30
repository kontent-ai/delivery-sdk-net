using System;
using System.Net;
using System.Text.RegularExpressions;
using Kentico.Kontent.Delivery.Abstractions.ContentLinks;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentLinks
{
    internal sealed class ContentLinkResolver
    {
        private static readonly Regex ElementRegex = new Regex("<a[^>]+?data-item-id=\"(?<id>[^\"]+)\"[^>]*>", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public IContentLinkUrlResolver ContentLinkUrlResolver { get; }

        public ContentLinkResolver(IContentLinkUrlResolver contentLinkUrlResolver)
        {
            ContentLinkUrlResolver = contentLinkUrlResolver ?? throw new ArgumentNullException(nameof(contentLinkUrlResolver));
        }

        public string ResolveContentLinks(string text, JToken links)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (links == null)
            {
                throw new ArgumentNullException(nameof(links));
            }

            if (text == string.Empty)
            {
                return text;
            }

            return ElementRegex.Replace(text, match =>
            {
                var contentItemId = match.Groups["id"].Value;
                var linkSource = links[contentItemId];

                if (linkSource == null)
                {
                    return ResolveMatch(match, ContentLinkUrlResolver.ResolveBrokenLinkUrl());
                }

                var link = new ContentLink(contentItemId, linkSource);

                return ResolveMatch(match, ContentLinkUrlResolver.ResolveLinkUrl(link));
            });
        }

        private string ResolveMatch(Match match, string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return match.Value;
            }

            const string needle = "href=\"\"";
            var haystack = match.Value;
            var index = haystack.IndexOf(needle, StringComparison.InvariantCulture);
            
            if (index < 0)
            {
                return haystack;
            }

            return haystack.Insert(index + 6, WebUtility.HtmlEncode(url)); 
        }
    }
}
