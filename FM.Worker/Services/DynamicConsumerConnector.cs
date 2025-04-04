using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.DependencyInjection;
using FM.Worker.Services.Api;
using FM.Worker.Services.Consumers;

namespace FM.Worker.Services
{
    public class DynamicConsumerConnector
    {
        public void Configure(IRabbitMqBusFactoryConfigurator configurator, IServiceProvider serviceProvider)
        {
            var tenantRegistry = serviceProvider.GetRequiredService<ITenantEndpointRegistry>();
            var apiClient = serviceProvider.GetRequiredService<IApiGatewayClient>();

            // Get all active tenants and their configurations
            var tenants = apiClient.GetActiveTenantsAsync().GetAwaiter().GetResult();
            
            foreach (var tenant in tenants)
            {
                var configs = apiClient.GetActiveConfigurationsAsync(tenant.Id).GetAwaiter().GetResult();
                
                foreach (var config in configs)
                {
                    // Create a unique queue name for this tenant+config combination
                    string queueName = tenantRegistry.GetQueueName(tenant.Id, config.Id);
                    
                    // Configure the receive endpoint
                    configurator.ReceiveEndpoint(queueName, e =>
                    {
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                        e.PrefetchCount = 4;
                        e.ConfigureConsumer<FileProcessingConsumer>(serviceProvider as IRegistrationContext);
                    });
                }
            }
        }
    }
} 