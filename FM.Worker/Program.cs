using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using FM.Worker.Services;
using FM.Worker.Services.Api;
using FM.Worker.Services.Consumers;
using MassTransit;
using Quartz;
using System;
using MassTransit.RabbitMqTransport;

namespace FM.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    
                    // Configure API Gateway options
                    services.Configure<ApiGatewayOptions>(hostContext.Configuration.GetSection("ApiGateway"));
                    
                    // Register HttpClient for API Gateway
                    services.AddHttpClient<IApiGatewayClient, ApiGatewayClient>((serviceProvider, client) => 
                    {
                        var options = hostContext.Configuration.GetSection("ApiGateway").Get<ApiGatewayOptions>();
                        client.BaseAddress = new Uri(options?.BaseUrl ?? throw new InvalidOperationException("API Gateway base URL is not configured"));
                        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                    });
                    
                    // Register singleton services for topology management
                    services.AddSingleton<ITenantEndpointRegistry, TenantEndpointRegistry>();
                    services.AddSingleton<ICustomRabbitMqBusTopology, RabbitMqBusTopology>();
                    
                    // Add MassTransit with RabbitMQ
                    services.AddMassTransit(x => 
                    {
                        // Register consumers
                        x.AddConsumer<FileProcessingConsumer>();
                        
                        x.UsingRabbitMq((context, cfg) => 
                        {
                            // Host configuration
                            var rabbitMqConfig = hostContext.Configuration.GetSection("RabbitMq");
                            cfg.Host(rabbitMqConfig["Host"], h => 
                            {
                                h.Username(rabbitMqConfig["Username"] ?? throw new InvalidOperationException("RabbitMQ username is not configured"));
                                h.Password(rabbitMqConfig["Password"] ?? throw new InvalidOperationException("RabbitMQ password is not configured"));
                            });
                            
                            // Base dead letter queue for all file processing
                            cfg.ReceiveEndpoint("file-processing-deadletter", e => 
                            {
                                e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                                e.PrefetchCount = 4;
                            });
                            
                            // Get active tenants and configurations to set up initial endpoints
                            var apiClient = context.GetRequiredService<IApiGatewayClient>();
                            var tenantRegistry = context.GetRequiredService<ITenantEndpointRegistry>();
                            var busTopology = context.GetRequiredService<ICustomRabbitMqBusTopology>();
                            
                            // Initialize the dynamic queue creation
                            busTopology.Initialize(cfg, new FileProcessingConsumerDefinition(), new DynamicConsumerConnector(), context);
                        });
                    });
                    
                    // Add Quartz.NET for job scheduling
                    services.AddQuartz(q =>
                    {
                        // Add job storage - use AdoJobStore in production for clustered environment
                        q.UseInMemoryStore();
                        
                        // Register FileProcessingJob
                        q.AddJob<FileProcessingJob>(opts => opts
                            .WithIdentity("FileProcessingJob")
                            .StoreDurably());
                    });
                    
                    // Add the Quartz.NET hosted service
                    services.AddQuartzHostedService(q => 
                    {
                        q.WaitForJobsToComplete = true;
                    });
                    
                    // Register API client
                    services.AddSingleton<IApiGatewayClient, ApiGatewayClient>();
                });
    }
}