using System.Collections.Generic;

namespace FM.Worker.Services
{
    /// <summary>
    /// Registry for tracking firm-specific endpoints in RabbitMQ
    /// </summary>
    public interface ITenantEndpointRegistry
    {
        /// <summary>
        /// Gets or creates a queue name for the given firm and client
        /// </summary>
        string GetQueueName(string firmCode, string clientCode);

        /// <summary>
        /// Checks if an endpoint is registered for the given firm and config
        /// </summary>
        bool IsEndpointRegistered(string firmCode, int configId);
        
        /// <summary>
        /// Registers an endpoint for a firm and config combination
        /// </summary>
        void RegisterEndpoint(string firmCode, int configId, string queueName);
        
        /// <summary>
        /// Gets the endpoint name for a firm and config combination
        /// </summary>
        string GetEndpointName(string firmCode, int configId);
        
        /// <summary>
        /// Gets all registered endpoint names
        /// </summary>
        IEnumerable<string> GetAllEndpoints();
    }
}