using ApiGateway;
using Azure.Identity;
using Enmeshed.Tooling.Extensions;
using Microsoft.AspNetCore;

CreateWebHostBuilder(args).Build().Run();

static IWebHostBuilder CreateWebHostBuilder(string[] args)
{
    var builder = WebHost.CreateDefaultBuilder(args);

    return builder
        .UseKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = 20.Mebibytes();
        })
        .ConfigureServices(s => s.AddSingleton(builder))
        .ConfigureAppConfiguration(AddAzureAppConfiguration)
        .UseStartup<Startup>();
}

static void AddAzureAppConfiguration(WebHostBuilderContext hostingContext, IConfigurationBuilder builder)
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
