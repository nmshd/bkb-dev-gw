using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace ApiGateway
{
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

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

            return httpContext.Response.WriteAsync(JsonSerializer.Serialize(responseObject, new JsonSerializerOptions {WriteIndented = true}));
        }
    }

    public class ServiceHealthCheck : IHealthCheck
    {
        private static readonly HttpClient HTTP_CLIENT = new();
        private readonly IConfiguration _configuration;

        public ServiceHealthCheck(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var serviceHealthEndpoints = _configuration.GetSection("HealthCheck:Urls").Get<string[]>();

            var tasks = serviceHealthEndpoints.Select(async serviceHealthEndpoint =>
            {
                var serviceHost = new Uri(serviceHealthEndpoint).Host;

                try
                {
                    var result = await HTTP_CLIENT.GetAsync(serviceHealthEndpoint, cancellationToken);

                    return
                        new ServiceHealth
                        {
                            HealthCheckEndpoint = serviceHost,
                            StatusCode = result.StatusCode,
                            Status = result.StatusCode == HttpStatusCode.OK ? "Healthy" : "Unhealthy",
                            Description = await result.Content.ReadAsStringAsync(cancellationToken)
                        };
                }
                catch (HttpRequestException ex)
                {
                    return
                        new ServiceHealth
                        {
                            HealthCheckEndpoint = serviceHost,
                            StatusCode = ex.StatusCode,
                            Status = "Unhealthy",
                            Description = "An unexpected error occured."
                        };
                }
            }).ToList();

            var healthCheckResults = await Task.WhenAll(tasks);

            var resultData = healthCheckResults.ToDictionary(r => r.HealthCheckEndpoint, r => (object) new {r.Status});


            return healthCheckResults.All(r => r.StatusCode == HttpStatusCode.OK) ? HealthCheckResult.Healthy("", resultData) : HealthCheckResult.Unhealthy("", null, resultData);
        }
    }

    public class ServiceHealth
    {
        public string HealthCheckEndpoint { get; set; }
        public HttpStatusCode? StatusCode { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
    }
}
