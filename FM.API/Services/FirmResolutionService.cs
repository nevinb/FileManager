using FM.API.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FM.API.Services
{
    public class FirmResolutionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FirmResolutionService> _logger;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

        public FirmResolutionService(
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IMemoryCache cache,
            ILogger<FirmResolutionService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
        }

        public FirmConfiguration GetCurrentFirmConfiguration()
        {
            var firmCode = GetCurrentFirmCode();
            return GetFirmConfiguration(firmCode);
        }

        public string GetCurrentClientCode()
        {
            // First try to get client code from header
            var clientCode = _httpContextAccessor.HttpContext?.Request.Headers["X-ClientCode"].ToString();
            
            if (string.IsNullOrEmpty(clientCode))
            {
                // If client code not provided, use firm code as client code
                clientCode = GetCurrentFirmCode();
                _logger.LogInformation("Client code not specified, using firm code {FirmCode} as client code", clientCode);
            }
            
            return clientCode;
        }

        public FirmConfiguration GetFirmConfiguration(string firmCode)
        {
            if (string.IsNullOrEmpty(firmCode))
            {
                _logger.LogWarning("No firm code provided");
                throw new ArgumentException("Firm code must be provided");
            }

            // Try to get from cache
            string cacheKey = $"FirmConfig_{firmCode}";
            if (!_cache.TryGetValue(cacheKey, out FirmConfiguration? firmConfig))
            {
                // Not in cache, load from configuration source
                firmConfig = LoadFirmConfiguration(firmCode);

                if (firmConfig == null)
                {
                    _logger.LogError("Firm configuration not found for firm code: {FirmCode}", firmCode);
                    throw new KeyNotFoundException($"Configuration for firm {firmCode} not found");
                }

                // Store in cache
                _cache.Set(cacheKey, firmConfig, _cacheDuration);
            }

            return firmConfig!;
        }

        public string GetCurrentFirmCode()
        {
            // Get firm code from request header
            var firmCode = _httpContextAccessor.HttpContext?.Request.Headers["X-FirmCode"].ToString();
            
            if (string.IsNullOrEmpty(firmCode))
            {
                // Try from route data
                if (_httpContextAccessor.HttpContext?.Request.RouteValues.TryGetValue("firmCode", out var routeFirmCode) == true)
                {
                    firmCode = routeFirmCode?.ToString();
                }
            }
            
            if (string.IsNullOrEmpty(firmCode))
            {
                // Try from subdomain
                var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
                if (!string.IsNullOrEmpty(host) && host.Contains('.'))
                {
                    var subdomain = host.Split('.')[0];
                    if (!string.IsNullOrEmpty(subdomain) && subdomain != "www")
                    {
                        firmCode = subdomain;
                    }
                }
            }
            
            if (string.IsNullOrEmpty(firmCode))
            {
                // Fallback to default firm for development/testing only
                if (_configuration["DOTNET_ENVIRONMENT"] == "Development")
                {
                    firmCode = "firm-xyz"; // Default for testing - shared DB
                    _logger.LogWarning("Using default firm code 'firm-xyz' in development environment");
                }
                else
                {
                    _logger.LogError("Unable to resolve firm code");
                    throw new InvalidOperationException("Unable to resolve firm code");
                }
            }
            
            return firmCode;
        }

        private FirmConfiguration? LoadFirmConfiguration(string firmCode)
        {
            // This would typically come from a database or configuration service
            // For demo purposes, using hard-coded values
            
            Dictionary<string, FirmConfiguration> firmConfigs = new()
            {
                // Dedicated database firm
                ["firm-abc"] = new FirmConfiguration
                {
                    FirmCode = "firm-abc",
                    DatabaseMode = FirmDatabaseMode.Dedicated,
                    ConnectionStringName = "firm-abc-db",
                    Schema = "dbo",
                    AllowedClientCodes = new[] { "firm-abc" } // Can only use its own code as client
                },
                
                // Shared database firms
                ["firm-xyz"] = new FirmConfiguration
                {
                    FirmCode = "firm-xyz",
                    DatabaseMode = FirmDatabaseMode.Shared,
                    ConnectionStringName = "shared-db",
                    Schema = "firm_xyz",
                    AllowedClientCodes = new[] { "client-xyz-1", "client-xyz-2", "client-xyz-3" }
                },
                
                ["firm-def"] = new FirmConfiguration
                {
                    FirmCode = "firm-def",
                    DatabaseMode = FirmDatabaseMode.Shared,
                    ConnectionStringName = "shared-db",
                    Schema = "firm_def",
                    AllowedClientCodes = new[] { "client-def-1", "client-def-2" }
                }
            };

            return firmConfigs.TryGetValue(firmCode, out var config) ? config : null;
        }

        public bool ValidateClientForFirm(string clientCode, string firmCode)
        {
            // Special case: if client code equals firm code (default behavior when client code not provided),
            // then for dedicated DB firms this is always valid
            if (clientCode == firmCode)
            {
                var config = GetFirmConfiguration(firmCode);
                if (config.DatabaseMode == FirmDatabaseMode.Dedicated)
                {
                    _logger.LogInformation("Automatically allowing client code equal to firm code {FirmCode} for dedicated database", firmCode);
                    return true;
                }
            }
            
            var firmConfig = GetFirmConfiguration(firmCode);
            
            // For dedicated DB, client code must match firm code
            if (firmConfig.DatabaseMode == FirmDatabaseMode.Dedicated)
            {
                return clientCode == firmCode;
            }
            
            // For shared DB, check if client code is in allowed list
            var isAllowed = firmConfig.AllowedClientCodes.Contains(clientCode);
            
            if (!isAllowed)
            {
                _logger.LogWarning("Client code {ClientCode} is not in the allowed list for firm {FirmCode}", clientCode, firmCode);
            }
            
            return isAllowed;
        }
    }
}