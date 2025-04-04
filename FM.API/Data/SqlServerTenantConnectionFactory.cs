using FM.API.Models;
using FM.API.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Threading.Tasks;

namespace FM.API.Data
{
    public class SqlServerTenantConnectionFactory : ITenantConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly FirmResolutionService _firmResolutionService;
        private readonly ILogger<SqlServerTenantConnectionFactory> _logger;

        public SqlServerTenantConnectionFactory(
            IConfiguration configuration,
            FirmResolutionService firmResolutionService,
            ILogger<SqlServerTenantConnectionFactory> logger)
        {
            _configuration = configuration;
            _firmResolutionService = firmResolutionService;
            _logger = logger;
        }

        public async Task<IDbConnection> CreateConnectionAsync(string tenantId)
        {
            // Get firm configuration based on tenant ID (which is our firm code)
            var firmConfig = _firmResolutionService.GetFirmConfiguration(tenantId);
            var connectionString = _configuration.GetConnectionString(firmConfig.ConnectionStringName);
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string '{ConnectionString}' not found", firmConfig.ConnectionStringName);
                throw new InvalidOperationException($"Connection string '{firmConfig.ConnectionStringName}' not found");
            }
            
            var connection = new SqlConnection(connectionString);
            
            try
            {
                await connection.OpenAsync();
                _logger.LogDebug("Opened connection to {ConnectionStringName} for firm {FirmCode}", 
                    firmConfig.ConnectionStringName, firmConfig.FirmCode);
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open database connection to {ConnectionString}", 
                    firmConfig.ConnectionStringName);
                throw;
            }
        }

        // Helper methods to be used by repositories
        public string GetCurrentSchema()
        {
            var firmConfig = _firmResolutionService.GetCurrentFirmConfiguration();
            return firmConfig.Schema;
        }

        public string GetCurrentClientCode()
        {
            return _firmResolutionService.GetCurrentClientCode();
        }
    }
}