using MassTransit;

namespace FM.Worker.Services.Consumers
{
    public class FileProcessingConsumerDefinition : ConsumerDefinition<FileProcessingConsumer>
    {
        public FileProcessingConsumerDefinition()
        {
            // Set the endpoint name
            EndpointName = "file-processing";
            
            // Configure retry policy
            ConcurrentMessageLimit = 4;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<FileProcessingConsumer> consumerConfigurator)
        {
            // Configure message retry
            endpointConfigurator.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        }
    }
} 