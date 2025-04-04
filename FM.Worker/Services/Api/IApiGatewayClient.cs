using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FM.Worker.Models;

namespace FM.Worker.Services.Api
{
    public interface IApiGatewayClient
    {
        Task<FileTransferConfig> GetConfigurationAsync(string tenantId, int configId);
        Task<bool> IsFileProcessedAsync(string tenantId, int configId, string fileName, DateTime lastModified, long fileSize);
        Task MarkFileAsProcessedAsync(string tenantId, int configId, string fileName, DateTime lastModified, long fileSize);
        Task<IEnumerable<TenantInfo>> GetActiveTenantsAsync();
        Task<IEnumerable<ConfigurationInfo>> GetActiveConfigurationsAsync(string tenantId);
    }

    public class TenantInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class ConfigurationInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string DestinationType { get; set; } = string.Empty;
    }
}