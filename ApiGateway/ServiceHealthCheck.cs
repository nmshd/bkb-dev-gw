using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ApiGateway
{
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
