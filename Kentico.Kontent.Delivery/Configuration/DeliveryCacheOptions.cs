using System;
using System.Collections.Generic;
using System.Text;

namespace Kentico.Kontent.Delivery.Configuration
{
    public class DeliveryCacheOptions
    {
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan StaleContentExpiration { get; set; } = TimeSpan.FromSeconds(10);
    }
}
