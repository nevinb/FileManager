using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using MassTransit;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace FM.ServiceDefaults;

public static class Extensions
{
    public static IServiceCollection AddServiceDefaults(this IServiceCollection services)
    {
        // Add health check services
        services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy());

        // Add OpenTelemetry services with metrics and tracing
        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddAspNetCoreInstrumentation()
                       .AddProcessInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddEntityFrameworkCoreInstrumentation();
            });

        // Add common logging configuration
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        return services;
    }

    // Add a method for configuring MassTransit with production-ready health checks
    public static IServiceCollection AddMassTransitWithHealthChecks(this IServiceCollection services, Action<IBusRegistrationConfigurator> configureBus)
    {
        // Configure MassTransit with the provided bus configuration
        services.AddMassTransit(bus =>
        {
            // Configure MassTransit bus according to caller's needs
            configureBus(bus);

            // Use kebab-case formatter for queue names
            bus.SetKebabCaseEndpointNameFormatter();
        });

        // Register custom health checks for MassTransit
        services.AddHealthChecks()
            // Add MassTransit bus health check
            .AddCheck<MassTransitBusHealthCheck>(
                "masstransit-bus",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "ready", "masstransit" },
                timeout: TimeSpan.FromSeconds(5))
            // Add RabbitMQ connectivity check
            .AddCheck<RabbitMqHealthCheck>(
                "rabbitmq-messaging-system",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready", "messaging" },
                timeout: TimeSpan.FromSeconds(10));

        return services;
    }

    // Health check for MassTransit bus
    private class MassTransitBusHealthCheck : IHealthCheck
    {
        private readonly IBus _bus;
        private readonly ILogger<MassTransitBusHealthCheck>? _logger;

        public MassTransitBusHealthCheck(IBus bus, ILogger<MassTransitBusHealthCheck>? logger)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if the bus is in a valid state
                if (_bus.Address != null)
                {
                    var data = new Dictionary<string, object>
                    {
                        { "BusAddress", _bus.Address.ToString() }
                    };

                    // Safely get the MassTransit version
                    var assembly = typeof(IBus).Assembly;
                    var version = assembly.GetName().Version;
                    if (version != null)
                    {
                        data.Add("MassTransitVersion", version.ToString());
                    }

                    _logger?.LogInformation("MassTransit bus is connected at {BusAddress}", _bus.Address);
                    
                    return Task.FromResult(HealthCheckResult.Healthy(
                        "MassTransit bus is operational",
                        data));
                }
                
                return Task.FromResult(HealthCheckResult.Degraded(
                    "MassTransit bus is available but address is not assigned"));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred checking MassTransit bus health");
                
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Exception occurred checking MassTransit bus health", 
                    ex));
            }
        }
    }

    // Health check for RabbitMQ
    private class RabbitMqHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMqHealthCheck>? _logger;

        public RabbitMqHealthCheck(IServiceProvider serviceProvider, ILogger<RabbitMqHealthCheck>? logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if the bus is registered and available
                var bus = _serviceProvider.GetService(typeof(IBus)) as IBus;
                
                if (bus == null)
                {
                    _logger?.LogWarning("RabbitMQ health check failed: IBus not registered");
                    
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        "MassTransit IBus is not registered"));
                }

                // Check if the bus has a valid address (indicates connection to RabbitMQ)
                if (bus.Address == null)
                {
                    _logger?.LogWarning("RabbitMQ health check failed: IBus has no address");
                    
                    return Task.FromResult(HealthCheckResult.Degraded(
                        "RabbitMQ connection may not be fully established"));
                }

                // Add connection details to the health check data
                var data = new Dictionary<string, object>
                {
                    { "BusAddress", bus.Address.ToString() },
                    { "ConnectionTime", DateTimeOffset.UtcNow.ToString("o") }
                };
                
                _logger?.LogInformation("RabbitMQ connection is healthy at {BusAddress}", bus.Address);
                
                return Task.FromResult(HealthCheckResult.Healthy(
                    "RabbitMQ connection is available",
                    data));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred checking RabbitMQ health");
                
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Exception occurred checking RabbitMQ connection", 
                    ex));
            }
        }
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Map health check endpoints with detailed response writer
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            AllowCachingResponses = false,
            ResponseWriter = WriteHealthCheckResponse
        });

        // Add separate health check endpoint for readiness (includes MassTransit endpoints)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            AllowCachingResponses = false,
            ResponseWriter = WriteHealthCheckResponse
        });

        // Add specific endpoint for MassTransit health
        app.MapHealthChecks("/health/masstransit", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("masstransit"),
            AllowCachingResponses = false,
            ResponseWriter = WriteHealthCheckResponse
        });

        return app;
    }

    // Custom health check response writer for detailed JSON output
    private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        
        // Set appropriate status code based on health status
        context.Response.StatusCode = report.Status switch
        {
            HealthStatus.Healthy => StatusCodes.Status200OK,
            HealthStatus.Degraded => StatusCodes.Status200OK, // Still return 200 for degraded but with degraded status in body
            HealthStatus.Unhealthy => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };
        
        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTimeOffset.UtcNow,
            components = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds,
                exception = e.Value.Exception?.Message,
                data = e.Value.Data.Count > 0 ? e.Value.Data : null
            })
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
    }
}
