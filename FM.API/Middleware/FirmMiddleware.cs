using FM.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FM.API.Middleware
{
    public class FirmMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FirmMiddleware> _logger;

        public FirmMiddleware(RequestDelegate next, ILogger<FirmMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, FirmResolutionService firmService)
        {
            try
            {
                // Skip firm resolution for specific paths
                if (context.Request.Path.StartsWithSegments("/health") ||
                    context.Request.Path.StartsWithSegments("/metrics") ||
                    context.Request.Path.StartsWithSegments("/swagger"))
                {
                    await _next(context);
                    return;
                }

                // Resolve firm code
                var firmCode = firmService.GetCurrentFirmCode();
                var firmConfig = firmService.GetFirmConfiguration(firmCode);

                // Get client code (will use firm code if client code not provided)
                var clientCode = firmService.GetCurrentClientCode();
                
                // Log whether we're using the firm code as client code
                if (clientCode == firmCode)
                {
                    _logger.LogInformation("Using firm code {FirmCode} as client code", firmCode);
                }

                // Validate client code belongs to this firm
                if (!firmService.ValidateClientForFirm(clientCode, firmCode))
                {
                    _logger.LogWarning("Unauthorized: Client code {ClientCode} not valid for firm {FirmCode}", 
                        clientCode, firmCode);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync($"Client code {clientCode} is not authorized for firm {firmCode}");
                    return;
                }

                _logger.LogInformation("Request for firm {FirmCode}, client {ClientCode} using {DatabaseMode} database mode", 
                    firmCode, clientCode, firmConfig.DatabaseMode);

                // Add firm and client info to the HttpContext items for use elsewhere
                context.Items["FirmCode"] = firmCode;
                context.Items["ClientCode"] = clientCode;
                context.Items["FirmConfig"] = firmConfig;

                // Continue with the request
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during firm/client resolution");
                
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("An error occurred processing your request");
            }
        }
    }

    // Extension method to make it easier to add the middleware
    public static class FirmMiddlewareExtensions
    {
        public static IApplicationBuilder UseFirmMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FirmMiddleware>();
        }
    }
}