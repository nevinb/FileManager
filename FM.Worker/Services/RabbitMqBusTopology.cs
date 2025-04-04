using MassTransit;
using MassTransit.RabbitMqTransport;
using FM.Worker.Services.Consumers;
using Microsoft.Extensions.DependencyInjection;

namespace FM.Worker.Services
{
    public interface ICustomRabbitMqBusTopology
    {
        void Initialize(IRabbitMqBusFactoryConfigurator configurator, IConsumerDefinition<FileProcessingConsumer> consumerDefinition, DynamicConsumerConnector consumerConnector, IServiceProvider serviceProvider);
    }

    public class RabbitMqBusTopology : ICustomRabbitMqBusTopology
    {
        public void Initialize(IRabbitMqBusFactoryConfigurator configurator, IConsumerDefinition<FileProcessingConsumer> consumerDefinition, DynamicConsumerConnector consumerConnector, IServiceProvider serviceProvider)
        {
            // Configure the consumer definition
            configurator.ReceiveEndpoint("file-processing", e =>
            {
                e.ConfigureConsumer<FileProcessingConsumer>(serviceProvider as IRegistrationContext);
            });

            // Set up the dynamic consumer connector
            consumerConnector.Configure(configurator, serviceProvider);
        }
    }
} 