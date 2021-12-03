using System;
using Azure.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var builder = WebHost.CreateDefaultBuilder(args);

            return builder
                .UseKestrel(options =>
                {
                    options.AddServerHeader = false;
                    options.Limits.MaxRequestBodySize = 20 * 1024 * 1024; // 20 MB
                })
                .ConfigureServices(s => s.AddSingleton(builder))
                .ConfigureAppConfiguration(AddAzureAppConfiguration)
                .UseStartup<Startup>();
        }

        private static void AddAzureAppConfiguration(WebHostBuilderContext hostingContext, IConfigurationBuilder builder)
        {
            var configuration = builder.Build();

            builder.AddJsonFile("routing.json", false, true);

            var azureAppConfigurationConfiguration = configuration.GetAzureAppConfigurationConfiguration();

            if (azureAppConfigurationConfiguration.Enabled)
                builder.AddAzureAppConfiguration(appConfigurationOptions =>
                {
                    var credentials = new ManagedIdentityCredential();

                    appConfigurationOptions
                        .Connect(new Uri(azureAppConfigurationConfiguration.Endpoint), credentials)
                        .ConfigureKeyVault(vaultOptions => { vaultOptions.SetCredential(credentials); })
                        .Select("*", "")
                        .Select("*", "ApiGateway");
                });
        }
    }
}
