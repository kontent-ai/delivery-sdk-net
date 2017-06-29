using Microsoft.Extensions.Configuration;

namespace KenticoCloud.Delivery
{
    internal static class Configuration
    {
        private static IConfiguration _configuration;

        static Configuration()
        {
            var builder = new ConfigurationBuilder()
                .Add(new NET45ConfigurationProvider());

            _configuration = builder.Build();
        }

        public static T GetValue<T>(string key, T defaultValue) => _configuration.GetValue(key, defaultValue);
    }
}
