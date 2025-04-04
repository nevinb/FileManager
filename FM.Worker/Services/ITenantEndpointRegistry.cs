using System.Collections.Generic;

namespace FM.Worker.Services
{
    /// <summary>
    /// Registry for tracking tenant-specific endpoints in RabbitMQ
    /// </summary>
    public interface ITenantEndpointRegistry
    {
        /// <summary>
        /// Gets or creates a queue name for the given tenant and config
        /// </summary>
        string GetQueueName(string tenantId, string configId);

        /// <summary>
        /// Checks if an endpoint is registered for the given tenant and config
        /// </summary>
        bool IsEndpointRegistered(string tenantId, int configId);
        
        /// <summary>
        /// Registers an endpoint for a tenant and config combination
        /// </summary>
        void RegisterEndpoint(string tenantId, int configId, string queueName);
        
        /// <summary>
        /// Gets the endpoint name for a tenant and config combination
        /// </summary>
        string GetEndpointName(string tenantId, int configId);
        
        /// <summary>
        /// Gets all registered endpoint names
        /// </summary>
        IEnumerable<string> GetAllEndpoints();
    }
}