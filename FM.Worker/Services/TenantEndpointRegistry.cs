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

        public string GetQueueName(string firmCode, string clientCode)
        {
            string key = GetEndpointKey(firmCode, clientCode);
            if (_endpointMap.TryGetValue(key, out string? queueName))
            {
                return queueName;
            }

            // Generate a new queue name if not found
            queueName = $"file-processing-{firmCode}-{clientCode}";
            RegisterEndpoint(firmCode, int.Parse(clientCode), queueName);
            return queueName;
        }

        public void RegisterEndpoint(string firmCode, int configId, string queueName)
        {
            string key = GetEndpointKey(firmCode, configId.ToString());
            if (_endpointMap.TryAdd(key, queueName))
            {
                _logger.LogInformation("Registered endpoint for firm {FirmCode}, config {ConfigId} with queue {QueueName}", 
                    firmCode, configId, queueName);
            }
            else
            {
                _logger.LogWarning("Endpoint for firm {FirmCode}, config {ConfigId} already registered", 
                    firmCode, configId);
            }
        }

        public void RemoveEndpoint(string firmCode, string configId)
        {
            string key = GetEndpointKey(firmCode, configId);
            if (_endpointMap.TryRemove(key, out var queueName))
            {
                _logger.LogInformation("Removed endpoint for firm {FirmCode}, config {ConfigId} with queue {QueueName}", 
                    firmCode, configId, queueName);
            }
        }

        public bool IsEndpointRegistered(string firmCode, int configId)
        {
            string key = GetEndpointKey(firmCode, configId.ToString());
            return _endpointMap.ContainsKey(key);
        }

        public string GetEndpointName(string firmCode, int configId)
        {
            string key = GetEndpointKey(firmCode, configId.ToString());
            return _endpointMap.TryGetValue(key, out var queueName) ? queueName : string.Empty;
        }

        public IEnumerable<string> GetAllEndpoints()
        {
            return _endpointMap.Values;
        }
        
        private static string GetEndpointKey(string firmCode, string configId)
        {
            return $"{firmCode}:{configId}";
        }
    }
}