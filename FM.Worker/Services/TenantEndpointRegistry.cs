using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace FM.Worker.Services
{
    public class TenantEndpointRegistry : ITenantEndpointRegistry
    {
        private readonly ConcurrentDictionary<string, string> _endpointMap = new();
        private readonly ILogger<TenantEndpointRegistry> _logger;

        public TenantEndpointRegistry(ILogger<TenantEndpointRegistry> logger)
        {
            _logger = logger;
        }

        public string GetQueueName(string tenantId, string configId)
        {
            string key = GetEndpointKey(tenantId, configId);
            if (_endpointMap.TryGetValue(key, out string? queueName))
            {
                return queueName;
            }

            // Generate a new queue name if not found
            queueName = $"file-processing-{tenantId}-{configId}";
            RegisterEndpoint(tenantId, int.Parse(configId), queueName);
            return queueName;
        }

        public void RegisterEndpoint(string tenantId, int configId, string queueName)
        {
            string key = GetEndpointKey(tenantId, configId.ToString());
            if (_endpointMap.TryAdd(key, queueName))
            {
                _logger.LogInformation("Registered endpoint for tenant {TenantId}, config {ConfigId} with queue {QueueName}", 
                    tenantId, configId, queueName);
            }
            else
            {
                _logger.LogWarning("Endpoint for tenant {TenantId}, config {ConfigId} already registered", 
                    tenantId, configId);
            }
        }

        public void RemoveEndpoint(string tenantId, string configId)
        {
            string key = GetEndpointKey(tenantId, configId);
            if (_endpointMap.TryRemove(key, out var queueName))
            {
                _logger.LogInformation("Removed endpoint for tenant {TenantId}, config {ConfigId} with queue {QueueName}", 
                    tenantId, configId, queueName);
            }
        }

        public bool IsEndpointRegistered(string tenantId, int configId)
        {
            string key = GetEndpointKey(tenantId, configId.ToString());
            return _endpointMap.ContainsKey(key);
        }

        public string GetEndpointName(string tenantId, int configId)
        {
            string key = GetEndpointKey(tenantId, configId.ToString());
            return _endpointMap.TryGetValue(key, out var queueName) ? queueName : string.Empty;
        }

        public IEnumerable<string> GetAllEndpoints()
        {
            return _endpointMap.Values;
        }
        
        private static string GetEndpointKey(string tenantId, string configId)
        {
            return $"{tenantId}:{configId}";
        }
    }
}