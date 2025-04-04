using Aspire.Hosting;
using Microsoft.Extensions.Configuration;

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

// Add RabbitMQ for messaging
var messaging = builder.AddRabbitMQ("messaging");

// Set RabbitMQ environment variables for services
string rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
string rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";

// Configure service references with environment variables
api.WithReference(messaging)
   .WithEnvironment("RabbitMQ__Username", rabbitMqUsername)
   .WithEnvironment("RabbitMQ__Password", rabbitMqPassword);

worker.WithReference(messaging)
      .WithEnvironment("RabbitMQ__Username", rabbitMqUsername)
      .WithEnvironment("RabbitMQ__Password", rabbitMqPassword);

// Build and run the application
builder.Build().Run();
