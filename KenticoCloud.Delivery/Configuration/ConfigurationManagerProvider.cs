using Microsoft.Extensions.Configuration;

#if (NET45)
using System.Configuration;
#endif

namespace KenticoCloud.Delivery
{
    internal class ConfigurationManagerProvider : ConfigurationProvider, IConfigurationSource
    {
#if (NET45)
        public override void Load()
        {
            foreach (var settingKey in ConfigurationManager.AppSettings.AllKeys)
            {
                Data.Add(settingKey, ConfigurationManager.AppSettings[settingKey]);
            }
        }
#endif

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return this;
        }
    }
}
