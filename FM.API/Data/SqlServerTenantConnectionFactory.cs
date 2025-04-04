using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FM.API.Data
{
    public class SqlServerTenantConnectionFactory : ITenantConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SqlServerTenantConnectionFactory> _logger;
        private readonly ConcurrentDictionary<string, string> _connectionStringCache;

        public SqlServerTenantConnectionFactory(
            IConfiguration configuration,
            ILogger<SqlServerTenantConnectionFactory> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionStringCache = new ConcurrentDictionary<string, string>();
        }

        public async Task<IDbConnection> CreateConnectionAsync(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            }

            try
            {
                // Get connection string for the tenant (using cache for performance)
                string connectionString = _connectionStringCache.GetOrAdd(tenantId, id => GetTenantConnectionString(id));
                
                // Create and open a new connection
                var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create database connection for tenant {TenantId}", tenantId);
                throw new Exception($"Failed to connect to database for tenant {tenantId}", ex);
            }
        }

        private string GetTenantConnectionString(string tenantId)
        {
            // First try to get a tenant-specific connection string
            string specificConnString = _configuration[$"ConnectionStrings:Tenant_{tenantId}"];
            if (!string.IsNullOrEmpty(specificConnString))
            {
                _logger.LogDebug("Using specific connection string for tenant {TenantId}", tenantId);
                return specificConnString;
            }
            
            // Fall back to a template with tenant placeholder
            string template = _configuration["ConnectionStrings:TenantTemplate"];
            if (string.IsNullOrEmpty(template))
            {
                throw new InvalidOperationException(
                    $"No connection string found for tenant {tenantId} and no template defined");
            }
            
            _logger.LogDebug("Using connection string template for tenant {TenantId}", tenantId);
            return template.Replace("{tenantId}", tenantId);
        }
    }
}