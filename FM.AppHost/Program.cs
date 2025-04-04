using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Aspire.Hosting.RabbitMQ;

var builder = DistributedApplication.CreateBuilder(args);

// Add the API project
var api = builder.AddProject<Projects.FM_API>("api")
    .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName);

// Add the API Gateway project
var gateway = builder.AddProject<Projects.FM_API_Gateway>("api-gateway")
    .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName);

// Add the worker service
var worker = builder.AddProject<Projects.FM_Worker>("worker")
    .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName);

// Add the UI project with reference to the API Gateway
var ui = builder.AddProject<Projects.FM_UI>("ui")
    .WithReference(gateway)
    .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName);

// Determine if using local RabbitMQ or containerized version
bool useLocalRabbitMQ = builder.Configuration.GetValue<bool>("UseLocalRabbitMQ", false);

// RabbitMQ configuration
string rabbitMqHost;
string rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
string rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
string rabbitMqPort = builder.Configuration["RabbitMQ:Port"] ?? "5672";

// Either use local RabbitMQ or add containerized RabbitMQ
IResourceBuilder<RabbitMQServerResource>? messaging = null;
if (useLocalRabbitMQ)
{
    // When using local RabbitMQ, we don't add it as a resource
    // Just set the host to localhost
    rabbitMqHost = "localhost";
    Console.WriteLine("Using locally hosted RabbitMQ instance");
}
else
{
    // Add containerized RabbitMQ
    messaging = builder.AddRabbitMQ("messaging");
    rabbitMqHost = "{messaging.Name}";
    Console.WriteLine("Using containerized RabbitMQ instance");
}

// Configure service references with environment variables
api.WithEnvironment("RabbitMQ__Host", rabbitMqHost)
   .WithEnvironment("RabbitMQ__Port", rabbitMqPort)
   .WithEnvironment("RabbitMQ__Username", rabbitMqUsername)
   .WithEnvironment("RabbitMQ__Password", rabbitMqPassword);

worker.WithEnvironment("RabbitMQ__Host", rabbitMqHost)
      .WithEnvironment("RabbitMQ__Port", rabbitMqPort)
      .WithEnvironment("RabbitMQ__Username", rabbitMqUsername)
      .WithEnvironment("RabbitMQ__Password", rabbitMqPassword);

// Add references to messaging only if using containerized RabbitMQ
if (messaging != null)
{
    api.WithReference(messaging);
    worker.WithReference(messaging);
}

// Build and run the application
builder.Build().Run();
