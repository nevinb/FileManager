using System;
using System.IO;
using System.Threading.Tasks;
using FM.Worker.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FM.Worker.Services.Consumers
{
    public class FileProcessingConsumer : IConsumer<FileDetectedEvent>
    {
        private readonly ILogger<FileProcessingConsumer> _logger;

        public FileProcessingConsumer(ILogger<FileProcessingConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<FileDetectedEvent> context)
        {
            var message = context.Message;
            
            _logger.LogInformation("Processing file {FileName} for tenant {TenantId}, config {ConfigId}", 
                message.FileName, message.TenantId, message.ConfigId);
            
            try
            {
                // Process the file based on the configuration
                if (message.DestinationType.Equals("DFS", StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessDfsDestinationAsync(message);
                }
                else if (message.DestinationType.Equals("SFTP", StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessSftpDestinationAsync(message);
                }
                else
                {
                    _logger.LogWarning("Unsupported destination type: {DestinationType}", 
                        message.DestinationType);
                }
                
                _logger.LogInformation("Successfully processed file {FileName} for tenant {TenantId}, config {ConfigId}", 
                    message.FileName, message.TenantId, message.ConfigId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file {FileName} for tenant {TenantId}, config {ConfigId}", 
                    message.FileName, message.TenantId, message.ConfigId);
                
                // Rethrow to trigger retry logic
                throw;
            }
        }
        
        private async Task ProcessDfsDestinationAsync(FileDetectedEvent message)
        {
            try
            {
                // Create destination directory if it doesn't exist
                Directory.CreateDirectory(message.DestinationLocation);
                
                // Construct destination path
                string destinationPath = Path.Combine(
                    message.DestinationLocation, 
                    message.FileName);
                    
                _logger.LogInformation("Copying file from {SourcePath} to {DestinationPath}", 
                    message.FilePath, destinationPath);
                
                // Copy the file
                File.Copy(message.FilePath, destinationPath, true);
                
                _logger.LogInformation("Copied file to {DestinationPath}", destinationPath);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file to DFS destination: {Message}", ex.Message);
                throw;
            }
        }
        
        private async Task ProcessSftpDestinationAsync(FileDetectedEvent message)
        {
            _logger.LogInformation("SFTP destination processing for {FileName}", message.FileName);
            // Implement SFTP upload logic
            // This would use an SFTP client to upload the file to the SFTP server
            
            _logger.LogInformation("SFTP implementation not completed yet");
            await Task.Delay(500); // Placeholder
        }
    }
}