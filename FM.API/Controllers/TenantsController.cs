using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FM.API.Controllers
{
    [ApiController]
    [Route("api/tenants")]
    public class TenantsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TenantsController> _logger;

        public TenantsController(
            IConfiguration configuration,
            ILogger<TenantsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("active")]
        public ActionResult<IEnumerable<string>> GetActiveTenants()
        {
            try
            {
                // In a real implementation, this would come from a database or tenant registry
                // For demo purposes, we'll read from configuration
                var tenantsSection = _configuration.GetSection("Tenants:Active");
                if (tenantsSection == null || !tenantsSection.Exists())
                {
                    return new List<string> { "tenant1", "tenant2", "tenant3" }; // Default tenants
                }
                
                var tenants = tenantsSection.Get<List<string>>();
                return Ok(tenants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active tenants");
                return StatusCode(500, "An error occurred while retrieving active tenants");
            }
        }

        [HttpGet("{tenantId}/status")]
        public ActionResult<bool> GetTenantStatus(string tenantId)
        {
            try
            {
                // In a real implementation, this would check if the tenant is active
                // For demo purposes, we'll assume all tenants are active
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking tenant status for {TenantId}", tenantId);
                return StatusCode(500, "An error occurred while checking tenant status");
            }
        }
    }
}