using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.Sqlite;
using FM.ServiceDefaults;
using MassTransit;
using Microsoft.Extensions.Configuration;
using FM.API.Data;
using FM.API.Data.Models;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (health checks, telemetry, etc.)
builder.Services.AddServiceDefaults();

// Add controllers
builder.Services.AddControllers();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add database
builder.Services.AddScoped<SqliteConnection>(sp => 
    new SqliteConnection(builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=app.db"));

// Register repositories
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobHistoryRepository, JobHistoryRepository>();
builder.Services.AddScoped<DatabaseInitializer>();

// Register the tenant connection factory
builder.Services.AddSingleton<ITenantConnectionFactory, SqlServerTenantConnectionFactory>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add MassTransit with RabbitMQ and health checks
builder.Services.AddMassTransitWithHealthChecks(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        // Get connection from configuration or use default
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var username = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
        
        cfg.Host(host, h =>
        {
            h.Username(username);
            h.Password(password);
        });
        
        // Configure message endpoints here
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await dbInitializer.InitializeAsync();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS
app.UseCors("AllowAll");

// Map default health check endpoints
app.MapDefaultEndpoints();

// API endpoints
app.MapGet("/", () => "File Management API is running");

// Use controllers for API endpoints
app.MapControllers();

app.Run();
