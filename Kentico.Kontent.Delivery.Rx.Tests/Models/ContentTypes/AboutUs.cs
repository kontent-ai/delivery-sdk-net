// This code was generated by a kontent-generators-net tool 
// (see https://github.com/Kentico/kontent-generators-net).
// 
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated. 
// For further modifications of the class, create a separate file with the partial class.

using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;

namespace Kentico.Kontent.Delivery.Rx.Tests.Models.ContentTypes
{
    public class AboutUs
    {
        public const string Codename = "about_us";
        public const string FactsCodename = "facts";
        public const string UrlPatternCodename = "url_pattern";

        public IEnumerable<object> Facts { get; set; }
        public string UrlPattern { get; set; }
        public IContentItemSystemAttributes System { get; set; }
    }
}