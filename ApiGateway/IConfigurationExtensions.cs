using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace ApiGateway
{
    [ExcludeFromCodeCoverage]
    internal static class IConfigurationExtensions
    {
        public static AzureAppConfigurationConfiguration GetAzureAppConfigurationConfiguration(this IConfiguration configuration)
        {
            return new AzureAppConfigurationConfiguration(configuration);
        }
    }
    
    public class AzureAppConfigurationConfiguration
    {
        private readonly IConfigurationSection _azureAppConfigurationConfiguration;

        public AzureAppConfigurationConfiguration(IConfiguration configuration)
        {
            _azureAppConfigurationConfiguration = configuration.GetSection("AzureAppConfiguration");
        }

        public bool Enabled => !string.IsNullOrEmpty(ConnectionString + Endpoint); // when neither endpoint nor connection string are provided, app configuration is disabled
        public string ConnectionString => _azureAppConfigurationConfiguration["ConnectionString"] ?? "";
        public string Endpoint => _azureAppConfigurationConfiguration["Endpoint"] ?? "";
    }
}
