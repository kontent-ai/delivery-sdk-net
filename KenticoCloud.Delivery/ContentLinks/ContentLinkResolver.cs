using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace KenticoCloud.Delivery
{
    internal sealed class ContentLinkResolver
    {
        private readonly IContentLinkUrlResolver _linkUrlResolver;
        private static readonly Regex _elementRegex = new Regex("<a[^>]+?data-item-id=\"(?<id>[^\"]+)\"[^>]*>", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public ContentLinkResolver(IContentLinkUrlResolver linkUrlResolver)
        {
            if (linkUrlResolver == null)
            {
                throw new ArgumentNullException(nameof(linkUrlResolver));
            }

            _linkUrlResolver = linkUrlResolver;
        }

        public string ResolveContentLinks(string text, JObject links, ContentLinkUrlResolverContext context)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (links == null)
            {
                throw new ArgumentNullException(nameof(links));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (text == string.Empty)
            {
                return text;
            }

            return _elementRegex.Replace(text, match =>
            {
                var contentItemId = match.Groups["id"].Value;
                var linkSource = links[contentItemId];

                if (linkSource == null)
                {
                    return ResolveMatch(match, _linkUrlResolver.ResolveBrokenLinkUrl(context));
                }

                var link = new ContentLink(contentItemId, linkSource);

                return ResolveMatch(match, _linkUrlResolver.ResolveLinkUrl(link, context));
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
            var index = haystack.IndexOf(needle);
            
            if (index < 0)
            {
                return haystack;
            }

            return haystack.Insert(index + 6, WebUtility.HtmlEncode(url)); 
        }
    }
}
