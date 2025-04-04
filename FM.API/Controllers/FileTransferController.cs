using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FM.API.Data;
using FM.API.Data.Models;

namespace FM.API.Controllers
{
    [ApiController]
    [Route("api/filetransfer")]
    public class FileTransferController : ControllerBase
    {
        private readonly ITenantConnectionFactory _tenantConnectionFactory;
        private readonly ILogger<FileTransferController> _logger;

        public FileTransferController(
            ITenantConnectionFactory tenantConnectionFactory,
            ILogger<FileTransferController> logger)
        {
            _tenantConnectionFactory = tenantConnectionFactory;
            _logger = logger;
        }

        [HttpGet("configurations/{configId}")]
        public async Task<ActionResult<FileTransferConfig>> GetConfiguration(int configId)
        {
            try
            {
                // Get tenant ID from header
                if (!Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdValues))
                {
                    return BadRequest("Tenant ID is required in X-Tenant-ID header");
                }
                
                string tenantId = tenantIdValues.ToString();
                
                // Get database connection for the tenant
                using var connection = await _tenantConnectionFactory.CreateConnectionAsync(tenantId);
                
                // Use Dapper to fetch the configuration
                var sql = @"
                    SELECT Id, Name, SourceType, SourceLocation, 
                           DestinationType, DestinationLocation, 
                           CronSchedule, IsEnabled
                    FROM FileTransferConfig 
                    WHERE Id = @ConfigId";
                
                var config = await connection.QuerySingleOrDefaultAsync<FileTransferConfig>(
                    sql, new { ConfigId = configId });
                
                if (config == null)
                {
                    return NotFound($"Configuration with ID {configId} not found");
                }
                
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration {ConfigId}", configId);
                return StatusCode(500, "An error occurred while retrieving the configuration");
            }
        }

        [HttpGet("configurations/active")]
        public async Task<ActionResult<IEnumerable<FileTransferConfig>>> GetActiveConfigurations()
        {
            try
            {
                // Get tenant ID from header
                if (!Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdValues))
                {
                    return BadRequest("Tenant ID is required in X-Tenant-ID header");
                }
                
                string tenantId = tenantIdValues.ToString();
                
                // Get database connection for the tenant
                using var connection = await _tenantConnectionFactory.CreateConnectionAsync(tenantId);
                
                // Use Dapper to fetch active configurations
                var sql = @"
                    SELECT Id, Name, SourceType, SourceLocation, 
                           DestinationType, DestinationLocation, 
                           CronSchedule, IsEnabled
                    FROM FileTransferConfig 
                    WHERE IsEnabled = 1";
                
                var configs = await connection.QueryAsync<FileTransferConfig>(sql);
                
                return Ok(configs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active configurations");
                return StatusCode(500, "An error occurred while retrieving configurations");
            }
        }

        [HttpPost("configurations")]
        public async Task<ActionResult<FileTransferConfig>> CreateConfiguration([FromBody] FileTransferConfig config)
        {
            try
            {
                // Get tenant ID from header
                if (!Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdValues))
                {
                    return BadRequest("Tenant ID is required in X-Tenant-ID header");
                }
                
                string tenantId = tenantIdValues.ToString();
                
                // Get database connection for the tenant
                using var connection = await _tenantConnectionFactory.CreateConnectionAsync(tenantId);
                
                // Use Dapper to insert the configuration
                var sql = @"
                    INSERT INTO FileTransferConfig 
                    (Name, SourceType, SourceLocation, DestinationType, DestinationLocation, CronSchedule, IsEnabled) 
                    VALUES 
                    (@Name, @SourceType, @SourceLocation, @DestinationType, @DestinationLocation, @CronSchedule, @IsEnabled);
                    
                    SELECT CAST(SCOPE_IDENTITY() as int)";
                
                int id = await connection.QuerySingleAsync<int>(sql, config);
                
                config.Id = id;
                
                return CreatedAtAction(nameof(GetConfiguration), new { configId = id }, config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating configuration");
                return StatusCode(500, "An error occurred while creating the configuration");
            }
        }

        [HttpPut("configurations/{configId}")]
        public async Task<ActionResult> UpdateConfiguration(int configId, [FromBody] FileTransferConfig config)
        {
            try
            {
                if (configId != config.Id)
                {
                    return BadRequest("ID in route does not match ID in configuration");
                }
                
                // Get tenant ID from header
                if (!Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdValues))
                {
                    return BadRequest("Tenant ID is required in X-Tenant-ID header");
                }
                
                string tenantId = tenantIdValues.ToString();
                
                // Get database connection for the tenant
                using var connection = await _tenantConnectionFactory.CreateConnectionAsync(tenantId);
                
                // Use Dapper to update the configuration
                var sql = @"
                    UPDATE FileTransferConfig 
                    SET Name = @Name, 
                        SourceType = @SourceType, 
                        SourceLocation = @SourceLocation, 
                        DestinationType = @DestinationType, 
                        DestinationLocation = @DestinationLocation, 
                        CronSchedule = @CronSchedule, 
                        IsEnabled = @IsEnabled
                    WHERE Id = @Id";
                
                int rowsAffected = await connection.ExecuteAsync(sql, config);
                
                if (rowsAffected == 0)
                {
                    return NotFound($"Configuration with ID {configId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration {ConfigId}", configId);
                return StatusCode(500, "An error occurred while updating the configuration");
            }
        }

        [HttpGet("files/processed")]
        public async Task<ActionResult<bool>> IsFileProcessed([FromQuery] int configId, [FromQuery] string fileHash)
        {
            try
            {
                // Get tenant ID from header
                if (!Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdValues))
                {
                    return BadRequest("Tenant ID is required in X-Tenant-ID header");
                }
                
                string tenantId = tenantIdValues.ToString();
                
                // Get database connection for the tenant
                using var connection = await _tenantConnectionFactory.CreateConnectionAsync(tenantId);
                
                // Use Dapper to check if file is processed
                var sql = @"
                    SELECT COUNT(1) 
                    FROM ProcessedFiles 
                    WHERE ConfigId = @ConfigId AND FileHash = @FileHash";
                
                int count = await connection.ExecuteScalarAsync<int>(
                    sql, new { ConfigId = configId, FileHash = fileHash });
                
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file is processed");
                return StatusCode(500, "An error occurred while checking processed status");
            }
        }

        [HttpPost("files/processed")]
        public async Task<IActionResult> MarkFileAsProcessed([FromBody] ProcessedFile record)
        {
            try
            {
                // Get tenant ID from header
                if (!Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdValues))
                {
                    return BadRequest("Tenant ID is required in X-Tenant-ID header");
                }
                
                string tenantId = tenantIdValues.ToString();
                
                // Get database connection for the tenant
                using var connection = await _tenantConnectionFactory.CreateConnectionAsync(tenantId);
                
                // Use Dapper to insert processed file record
                var sql = @"
                    INSERT INTO ProcessedFiles (ConfigId, FileName, FileHash, FileModifiedDate, FileSize, ProcessedDate)
                    VALUES (@ConfigId, @FileName, @FileHash, @FileModifiedDate, @FileSize, @ProcessedDate)";
                
                await connection.ExecuteAsync(sql, record);
                
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking file as processed");
                return StatusCode(500, "An error occurred while marking file as processed");
            }
        }
    }
}