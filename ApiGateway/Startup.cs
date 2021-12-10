using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Logging;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace ApiGateway;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)

    {
        _configuration = configuration;
        _env = env;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc(options => options.EnableEndpointRouting = false);

        services.AddHealthChecks().AddCheck<ServiceHealthCheck>("Services");

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
        });

        services.AddOcelot(_configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseSecurityHeaders(policies =>
            policies
                .AddDefaultSecurityHeaders()
                .AddCustomHeader("Strict-Transport-Security", "max-age=5184000; includeSubDomains")
                .AddCustomHeader("X-Frame-Options", "Deny")
        );

        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse
        });

        if (_env.IsDevelopment())
        {
            app.UseCors("AllowAll");
            IdentityModelEventSource.ShowPII = true;
        }

        app.UseHttpsRedirection();

        app.UseOcelot().Wait();
    }

    private static Task WriteHealthCheckResponse(HttpContext httpContext, HealthReport result)
    {
        httpContext.Response.ContentType = "application/json";

        var responseObject = new
        {
            Status = result.Status.ToString(),
            Checks = result.Entries.Select(entry => new
            {
                entry.Key,
                Status = entry.Value.Status.ToString(),
                entry.Value.Description,
                entry.Value.Data
            })
        };

        return httpContext.Response.WriteAsync(JsonSerializer.Serialize(responseObject, new JsonSerializerOptions { WriteIndented = true }));
    }
}