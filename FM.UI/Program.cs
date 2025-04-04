using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FM.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (health checks, telemetry, etc.)
builder.Services.AddServiceDefaults();

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add HttpClient for API Gateway
builder.Services.AddHttpClient("ApiGateway", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ApiGateway"] ?? "http://api-gateway");
});

// Add application services
builder.Services.AddSingleton<FM.UI.Data.WeatherForecastService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Map default health check endpoints
app.MapDefaultEndpoints();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
