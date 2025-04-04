using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FM.Worker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FM.Worker.Services.Api
{
    public class ApiGatewayClient : IApiGatewayClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiGatewayClient> _logger;
        private readonly ApiGatewayOptions _options;

        public ApiGatewayClient(
            HttpClient httpClient,
            ILogger<ApiGatewayClient> logger,
            IOptions<ApiGatewayOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<FileTransferConfig> GetConfigurationAsync(string tenantId, int configId)
        {
            try
            {
                // Set tenant ID in request header
                using var request = new HttpRequestMessage(HttpMethod.Get, $"api/filetransfer/configurations/{configId}");
                request.Headers.Add("X-Tenant-ID", tenantId);
                
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadFromJsonAsync<FileTransferConfig>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration for tenant {TenantId}, config {ConfigId}", 
                    tenantId, configId);
                throw;
            }
        }

        public async Task<bool> IsFileProcessedAsync(string tenantId, int configId, string fileName, DateTime lastModified, long fileSize)
        {
            try
            {
                // Create file hash for checking
                string fileHash = ComputeFileHash(fileName, lastModified, fileSize);
                
                // Set tenant ID in request header
                using var request = new HttpRequestMessage(
                    HttpMethod.Get, 
                    $"api/filetransfer/files/processed?configId={configId}&fileHash={fileHash}");
                request.Headers.Add("X-Tenant-ID", tenantId);
                
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file is processed for tenant {TenantId}, config {ConfigId}", 
                    tenantId, configId);
                return false; // Assume not processed on error to be safe
            }
        }

        public async Task MarkFileAsProcessedAsync(string tenantId, int configId, string fileName, DateTime lastModified, long fileSize)
        {
            try
            {
                // Create file hash
                string fileHash = ComputeFileHash(fileName, lastModified, fileSize);
                
                // Create processed file record
                var processedFile = new
                {
                    ConfigId = configId,
                    FileName = fileName,
                    FileHash = fileHash,
                    FileModifiedDate = lastModified,
                    FileSize = fileSize,
                    ProcessedDate = DateTime.UtcNow
                };
                
                // Set tenant ID in request header
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/filetransfer/files/processed");
                request.Headers.Add("X-Tenant-ID", tenantId);
                request.Content = JsonContent.Create(processedFile);
                
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking file as processed for tenant {TenantId}, config {ConfigId}", 
                    tenantId, configId);
                throw;
            }
        }

        public async Task<IEnumerable<TenantInfo>> GetActiveTenantsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<TenantInfo>>("/api/tenants/active");
                return response ?? new List<TenantInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active tenants");
                return new List<TenantInfo>();
            }
        }

        public async Task<IEnumerable<ConfigurationInfo>> GetActiveConfigurationsAsync(string tenantId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ConfigurationInfo>>($"/api/tenants/{tenantId}/configurations/active");
                return response ?? new List<ConfigurationInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active configurations for tenant {TenantId}", tenantId);
                return new List<ConfigurationInfo>();
            }
        }

        private string ComputeFileHash(string fileName, DateTime lastModified, long fileSize)
        {
            // Create a deterministic hash based on file attributes
            string input = $"{fileName}|{lastModified:o}|{fileSize}";
            
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }
    }

    public class ApiGatewayOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
    }
}