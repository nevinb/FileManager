using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Quartz;
using FM.Worker.Models;
using FM.Worker.Services.Api;
using MassTransit;

namespace FM.Worker.Services
{
    [DisallowConcurrentExecution]
    public class FileProcessingJob : IJob
    {
        private readonly ILogger<FileProcessingJob> _logger;
        private readonly IApiGatewayClient _apiGatewayClient;
        private readonly IBus _bus;

        public FileProcessingJob(
            ILogger<FileProcessingJob> logger,
            IApiGatewayClient apiGatewayClient,
            IBus bus)
        {
            _logger = logger;
            _apiGatewayClient = apiGatewayClient;
            _bus = bus;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("FileProcessingJob started at {time}", DateTimeOffset.Now);
            
            try
            {
                // Get tenant and config information from job data
                string? tenantId = context.MergedJobDataMap.GetString("tenantId");
                int configId = context.MergedJobDataMap.GetInt("configId");
                
                if (string.IsNullOrEmpty(tenantId))
                {
                    _logger.LogError("TenantId not provided in job data");
                    return;
                }
                
                _logger.LogInformation("Processing files for tenant {TenantId}, config {ConfigId}", 
                    tenantId, configId);
                
                // Get configuration details from API
                var config = await _apiGatewayClient.GetConfigurationAsync(tenantId, configId);
                if (config == null || !config.IsEnabled)
                {
                    _logger.LogWarning("Configuration {ConfigId} not found or disabled for tenant {TenantId}", 
                        configId, tenantId);
                    return;
                }
                
                // Process based on source type
                switch (config.SourceType.ToUpperInvariant())
                {
                    case "DFS":
                        await ProcessDfsFilesAsync(config, tenantId);
                        break;
                    case "SFTP":
                        await ProcessSftpFilesAsync(config, tenantId);
                        break;
                    default:
                        _logger.LogWarning("Unknown source type: {SourceType}", config.SourceType);
                        break;
                }
                
                _logger.LogInformation("FileProcessingJob completed successfully at {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during file processing job");
                throw;
            }
        }
        
        private async Task ProcessDfsFilesAsync(FileTransferConfig config, string tenantId)
        {
            _logger.LogInformation("Processing DFS files for tenant {TenantId}, config {ConfigId} from {SourceLocation}", 
                tenantId, config.Id, config.SourceLocation);
            
            try
            {
                // Get files from DFS location
                var directory = new DirectoryInfo(config.SourceLocation);
                if (!directory.Exists)
                {
                    _logger.LogWarning("Source directory {Directory} does not exist", config.SourceLocation);
                    return;
                }
                
                var files = directory.GetFiles();
                _logger.LogInformation("Found {FileCount} files in {SourceLocation}", 
                    files.Length, config.SourceLocation);
                
                foreach (var file in files)
                {
                    // Check if file already processed via API
                    bool isProcessed = await _apiGatewayClient.IsFileProcessedAsync(
                        tenantId, 
                        config.Id, 
                        file.Name, 
                        file.LastWriteTimeUtc,
                        file.Length);
                        
                    if (isProcessed)
                    {
                        _logger.LogDebug("File {FileName} already processed", file.Name);
                        continue;
                    }
                    
                    // Create unique file identifier for routing and deduplication
                    string fileId = GenerateFileId(tenantId, config.Id, file);
                    
                    // Publish file detected event
                    await _bus.Publish(new FileDetectedEvent
                    {
                        TenantId = tenantId,
                        ConfigId = config.Id,
                        FilePath = file.FullName,
                        FileName = file.Name,
                        FileSize = file.Length,
                        LastModified = file.LastWriteTimeUtc,
                        SourceType = config.SourceType,
                        DestinationType = config.DestinationType,
                        DestinationLocation = config.DestinationLocation,
                        DetectedAt = DateTimeOffset.UtcNow
                    }, ctx => {
                        // Set message ID for deduplication - Convert to Guid for MassTransit
                        ctx.MessageId = new Guid(GenerateFileIdForGuid(tenantId, config.Id, file));
                        
                        // Set routing information in headers
                        ctx.Headers.Set("TenantId", tenantId);
                        ctx.Headers.Set("ConfigId", config.Id.ToString());
                    });
                    
                    _logger.LogInformation("Published file detection event for {FileName}", file.Name);
                    
                    // Mark file as processed via API
                    await _apiGatewayClient.MarkFileAsProcessedAsync(
                        tenantId,
                        config.Id,
                        file.Name,
                        file.LastWriteTimeUtc,
                        file.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DFS files: {Message}", ex.Message);
                throw;
            }
        }
        
        private async Task ProcessSftpFilesAsync(FileTransferConfig config, string tenantId)
        {
            _logger.LogInformation("Processing SFTP files for tenant {TenantId}, config {ConfigId} from {SourceLocation}", 
                tenantId, config.Id, config.SourceLocation);
            
            // Similar implementation as DFS but using an SFTP client
            // For now, we'll just use placeholder implementation
            await Task.Delay(500);
            
            _logger.LogInformation("SFTP processing not fully implemented yet");
        }
        
        private string GenerateFileId(string tenantId, int configId, FileInfo file)
        {
            // Create a deterministic hash combining tenant, config and file details
            string input = $"{tenantId}:{configId}:{file.FullName}:{file.LastWriteTimeUtc:o}:{file.Length}";
            
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }

        private string GenerateFileIdForGuid(string tenantId, int configId, FileInfo file)
        {
            // Create a deterministic hash combining tenant, config and file details
            string input = $"{tenantId}:{configId}:{file.FullName}:{file.LastWriteTimeUtc:o}:{file.Length}";
            
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "");
        }
    }
}