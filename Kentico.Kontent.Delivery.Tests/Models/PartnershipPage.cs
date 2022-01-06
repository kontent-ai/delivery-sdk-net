using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.Tests.Models;

public class PartnershipPage
{
    [JsonProperty("teaser__title")]
    public string TeaserTitle { get; set; }
}