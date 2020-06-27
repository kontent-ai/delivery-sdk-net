using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentItems.ContentLinks
{
    internal sealed class ContentLinkResolver
    {
        private static readonly Regex ElementRegex = new Regex("<a[^>]+?data-item-id=\"(?<id>[^\"]+)\"[^>]*>", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public IContentLinkUrlResolver ContentLinkUrlResolver { get; }

        public ContentLinkResolver(IContentLinkUrlResolver contentLinkUrlResolver)
        {
            ContentLinkUrlResolver = contentLinkUrlResolver ?? throw new ArgumentNullException(nameof(contentLinkUrlResolver));
        }

        public async Task<string> ResolveContentLinks(string text, JToken links)
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

            var matches = ElementRegex.Matches(text);

            static string Replace(string s, int index, int length, string replacement)
            {
                return string.Concat(s.Substring(0, index), replacement, s.Substring(index + length));
            }

            foreach (var match in matches.Cast<Match>().Reverse())
            {
                var contentItemId = match.Groups["id"].Value;
                var linkSource = links[contentItemId];

                string url;
                if (linkSource != null)
                {
                    var link = new ContentLink(contentItemId, linkSource);
                    url = await ContentLinkUrlResolver.ResolveLinkUrl(link);
                }
                else
                {
                    url = await ContentLinkUrlResolver.ResolveBrokenLinkUrl();
                }

                var replacement = ResolveMatch(match, url);
                text = Replace(text, match.Index, match.Length, replacement);
            }

            return text;
        }

        private static string ResolveMatch(Capture match, string url)
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

            var withinHrefIndex = index + needle.Length - "\"".Length;
            return haystack.Insert(withinHrefIndex, WebUtility.HtmlEncode(url));
        }
    }
}
