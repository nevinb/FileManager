# Context for GitHub Copilot
Create a cloud-native file management system using .NET Aspire with the following specifications:

# Project Overview
Create a solution named "FM" with the following characteristics:
- Microservices architecture using .NET Aspire
- Multi-tenant architecture with tenant isolation
- Support for DFS and SFTP file processing
- Dynamic job scheduling and monitoring
- Message-based processing with tenant-specific queues
- Cloud-native design principles

# Technical Requirements

## Architecture
- Implement microservices pattern with the following projects:
  1. FM.AppHost (Aspire host application)
  2. FM.ServiceDefaults (Common service configurations)
  3. FM.API.Gateway (Ocelot-based API Gateway with tenant routing)
  4. FM.API (Core API service with multi-tenant database support)
  5. FM.Worker (Background worker with tenant-specific job scheduling)
  6. FM.UI (Blazor Server UI)

## Multi-Tenant Architecture
- **Tenant Isolation**: Each tenant has a dedicated database
- **Header-Based Routing**: X-Tenant-ID header for tenant context propagation
- **Configuration ID Uniqueness**: Config IDs are unique within a tenant but can be duplicated across tenants
- **Tenant-Specific Queues**: Separate message queues for each tenant+config combination
- **Dynamic Endpoint Creation**: New queues created automatically for new configurations

## Core Technologies
- .NET 8.0
- Aspire for cloud-native capabilities
- Ocelot for API Gateway with tenant header routing
- RabbitMQ for messaging using MassTransit framework
- SQL Server for tenant-specific databases
- Dapper for data access with tenant context
- Quartz.NET for tenant-aware job scheduling
- MassTransit for message handling with tenant+config routing
- Autofac for dependency injection

## Tenant-Specific Message Routing
- Message headers include TenantId and ConfigId for routing
- Dynamic queue creation per tenant and configuration
- Tenant-specific consumers for message isolation
- Message filtering based on tenant+config context

## Design Patterns
- Repository Pattern for data access with tenant context
- Factory Pattern for dynamic tenant database connections
- Observer Pattern for configuration change detection
- Strategy Pattern for file processing based on source/destination type
- Registry Pattern for managing tenant endpoints
- Interface Segregation
- Single Responsibility
- SOLID principles throughout
- Cloud Native Design

## Testing Requirements
- MStest for unit testing
- Moq for mocking
- Minimum 80% code coverage
- Tests required for all business logic
- Tenant-specific test data isolation

# Functional Requirements
As an end user I should be able to configure a job which will execute every n sec.
The job primarily moves a file from 'X' location to 'Y' if a new/latest file is available.
'X' and 'Y' can be DFS or SFTP.

There should be 2 sections in the configuration screen:
- A From section with source configuration
- A To Section with destination configuration

Each configuration belongs to a specific tenant, with complete isolation between tenants.

# Key Implementation Details

## Tenant-Specific Database Connection Factory
```csharp
public async Task<IDbConnection> CreateConnectionAsync(string tenantId)
{
    // Get connection string for the tenant (using cache for performance)
    string connectionString = _connectionStringCache.GetOrAdd(tenantId, id => GetTenantConnectionString(id));
    
    // Create and open a new connection
    var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    
    return connection;
}
```

## Tenant-Specific API Controllers
```csharp
// Get tenant ID from header
if (!Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdValues))
{
    return BadRequest("Tenant ID is required in X-Tenant-ID header");
}

string tenantId = tenantIdValues.ToString();

// Get database connection for the tenant
using var connection = await _tenantConnectionFactory.CreateConnectionAsync(tenantId);
```

## Tenant-Specific Message Publishing
```csharp
// Publish with tenant context in headers
await _bus.Publish(new FileDetectedEvent
{
    TenantId = tenantId,
    ConfigId = config.Id,
    // Other properties...
}, ctx => {
    // Set routing information in headers
    ctx.Headers.Set("TenantId", tenantId);
    ctx.Headers.Set("ConfigId", config.Id.ToString());
});
```

## Tenant-Specific Queue Registration
```csharp
// Get or create queue name for tenant+config
string queueName = _endpointRegistry.GetQueueName(tenantId, configId);

// Connect the consumer for this tenant+config
await _consumerConnector.ConnectConsumer<FileProcessingConsumer, FileDetectedEvent>(
    _context, 
    _configurator, 
    queueName, 
    tenantId, 
    configId);
```

# Development Approach (6 Sprints)

## Sprint 1: Foundation Setup (2 weeks)
- Set up Aspire infrastructure
- Configure service defaults
- Implement multi-tenant database initialization
- Set up health checks and telemetry

## Sprint 2: Core API Implementation (2 weeks)
- Implement tenant-aware job management API
- Set up API Gateway with tenant header routing
- Configure multi-tenant service communication
- Implement basic CRUD operations with tenant isolation

## Sprint 3: Worker Service Implementation (2 weeks)
- Implement tenant-specific file processing logic
- Set up tenant-aware job scheduling
- Configure tenant-specific message queues and routing
- Implement file transfer services with tenant isolation

## Sprint 4: UI Implementation (2 weeks)
- Create tenant-aware job configuration UI
- Implement tenant-specific job status monitoring
- Add tenant-aware manual job triggering
- Create real-time updates with tenant context

## Sprint 5: Testing and Optimization (2 weeks)
- Implement comprehensive testing with tenant isolation
- Optimize performance for multi-tenant system
- Add tenant-specific monitoring and alerting
- Achieve code coverage targets

## Sprint 6: Deployment and Documentation (2 weeks)
- Prepare for production deployment with tenant isolation
- Complete multi-tenant documentation
- Perform security audit with focus on tenant isolation
- Create tenant-aware deployment guides

# Definition of Done
- Code follows project standards
- Multi-tenant isolation fully implemented
- Unit tests written and passing
- Integration tests written and passing
- Code reviewed and approved
- Documentation updated with tenant-specific details
- Builds successfully
- Deploys without errors
- Health checks pass for all tenants
- Monitoring configured with tenant context

# Architecture Rules
- No direct database access from UI or Worker
- No circular dependencies
- No tight coupling between services
- No hard-coded connection strings
- All configuration must be environment-based
- All services must implement health checks
- All public APIs must be documented
- All services must be containerizable
- Proper tenant isolation at all layers
- No cross-tenant data access allowed
- All inter-service communication must include tenant context in headers

# Security Requirements
- No secrets in code
- Use user secrets for local development
- Implement proper authentication with tenant context
- Use HTTPS everywhere
- Regular security scanning
- Proper error handling and logging
- Tenant data isolation enforced at all levels
- No tenant data leakage between requests

# Performance Requirements
- API response time < 200ms for tenant-specific operations
- Database query time < 100ms with tenant context
- Proper caching implementation with tenant isolation
- Async/await usage where appropriate
- Queue processing optimized for tenant-specific workloads

# Documentation Requirements
- XML comments on public APIs
- README.md in each project
- API documentation using Swagger with tenant context
- Architecture decision records (ADRs) for multi-tenant design
- Deployment and troubleshooting guides with tenant considerations