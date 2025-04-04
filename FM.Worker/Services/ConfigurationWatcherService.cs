using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FM.Worker.Services.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FM.Worker.Services
{
    public class ConfigurationWatcherService : BackgroundService
    {
        private readonly ILogger<ConfigurationWatcherService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IApiGatewayClient _apiGatewayClient;
        private readonly Dictionary<string, DateTime> _lastKnownConfigTimes = new Dictionary<string, DateTime>();

        public ConfigurationWatcherService(
            ILogger<ConfigurationWatcherService> logger,
            IServiceProvider serviceProvider,
            IApiGatewayClient apiGatewayClient)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _apiGatewayClient = apiGatewayClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ConfigurationWatcherService started at {Time}", DateTimeOffset.Now);
            
            try
            {
                // Initial load of all configurations
                await CheckForConfigurationChangesAsync(stoppingToken);
                
                // Continue checking periodically
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    await CheckForConfigurationChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error in ConfigurationWatcherService");
            }
            finally
            {
                _logger.LogInformation("ConfigurationWatcherService stopped at {Time}", DateTimeOffset.Now);
            }
        }

        private async Task CheckForConfigurationChangesAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Get all tenants
                var tenants = await _apiGatewayClient.GetActiveTenantsAsync();
                
                foreach (var tenant in tenants)
                {
                    // Get active configurations for this tenant
                    var configs = await _apiGatewayClient.GetActiveConfigurationsAsync(tenant.Id);
                    
                    // Process each configuration
                    foreach (var config in configs)
                    {
                        // Create a unique key for this tenant+config
                        string configKey = $"{tenant.Id}:{config.Id}";
                        
                        // Check if we've seen this configuration before
                        if (!_lastKnownConfigTimes.ContainsKey(configKey))
                        {
                            _logger.LogInformation("New configuration detected for tenant {TenantId}, config {ConfigId}", 
                                tenant.Id, config.Id);
                            
                            // Schedule a new job for this configuration
                            await ScheduleJobAsync(tenant.Id, config, stoppingToken);
                            
                            // Update last known time
                            _lastKnownConfigTimes[configKey] = DateTime.UtcNow;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for configuration changes");
            }
        }

        private async Task ScheduleJobAsync(string tenantId, ConfigurationInfo config, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var schedulerFactory = scope.ServiceProvider.GetRequiredService<ISchedulerFactory>();
                var scheduler = await schedulerFactory.GetScheduler(stoppingToken);
                
                // Create job data
                var jobData = new JobDataMap
                {
                    { "tenantId", tenantId },
                    { "configId", config.Id }
                };
                
                // Create job
                var jobKey = new JobKey($"FileTransferJob_{tenantId}_{config.Id}", "FileTransfer");
                var jobDetail = JobBuilder.Create<FileProcessingJob>()
                    .WithIdentity(jobKey)
                    .UsingJobData(jobData)
                    .StoreDurably()
                    .Build();
                
                // Create trigger with cron schedule (default to every 5 minutes)
                var triggerKey = new TriggerKey($"FileTransferTrigger_{tenantId}_{config.Id}", "FileTransfer");
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .WithCronSchedule("0 */5 * ? * *") // Every 5 minutes
                    .StartNow()
                    .Build();
                
                // Check if job already exists
                if (await scheduler.CheckExists(jobKey, stoppingToken))
                {
                    await scheduler.DeleteJob(jobKey, stoppingToken);
                }
                
                // Schedule the job
                await scheduler.ScheduleJob(jobDetail, trigger, stoppingToken);
                
                _logger.LogInformation("Scheduled file transfer job for tenant {TenantId}, config {ConfigId}", 
                    tenantId, config.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling job for tenant {TenantId}, config {ConfigId}", 
                    tenantId, config.Id);
            }
        }
    }
}