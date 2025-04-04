using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FM.API.Models;
using FM.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FM.API.Controllers
{
    [ApiController]
    [Route("api/firms")]
    public class TenantsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TenantsController> _logger;
        private readonly FirmResolutionService _firmResolutionService;

        public TenantsController(
            IConfiguration configuration,
            FirmResolutionService firmResolutionService,
            ILogger<TenantsController> logger)
        {
            _configuration = configuration;
            _firmResolutionService = firmResolutionService;
            _logger = logger;
        }

        [HttpGet("active")]
        public ActionResult<IEnumerable<string>> GetActiveFirms()
        {
            try
            {
                // In a real implementation, this would come from a database or firm registry
                // For demo purposes, we'll return the known firms from our configuration
                var firms = new List<string> { "firm-abc", "firm-xyz", "firm-def" };
                return Ok(firms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active firms");
                return StatusCode(500, "An error occurred while retrieving active firms");
            }
        }

        [HttpGet("{firmCode}/clients")]
        public ActionResult<IEnumerable<string>> GetFirmClients(string firmCode)
        {
            try
            {
                var firmConfig = _firmResolutionService.GetFirmConfiguration(firmCode);
                return Ok(firmConfig.AllowedClientCodes);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Firm with code {firmCode} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clients for firm {FirmCode}", firmCode);
                return StatusCode(500, "An error occurred while retrieving firm clients");
            }
        }

        [HttpGet("{firmCode}/status")]
        public ActionResult<object> GetFirmStatus(string firmCode)
        {
            try
            {
                // Get firm configuration to verify it exists and check its mode
                var firmConfig = _firmResolutionService.GetFirmConfiguration(firmCode);
                
                return new
                {
                    FirmCode = firmConfig.FirmCode,
                    Status = "Active",
                    DatabaseMode = firmConfig.DatabaseMode.ToString(),
                    Schema = firmConfig.Schema,
                    ClientCount = firmConfig.AllowedClientCodes.Length
                };
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Firm with code {firmCode} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking status for firm {FirmCode}", firmCode);
                return StatusCode(500, "An error occurred while checking firm status");
            }
        }
    }
}