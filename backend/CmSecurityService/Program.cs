using CmSecurityService.Data;
using CmSecurityService.Forwarding;
using CmSecurityService.Calls;
using CmSecurityService.Common;
using CmSecurityService.Incidents;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("database connection string is missing");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0))));

// Register View Incidents Service (#216)
builder.Services.AddIncidentViewing();

// Register 9-1-1 Call Forwarding Client & Service (#219)
builder.Services.AddHttpClient<IEmergencyServiceClient, EmergencyServiceClient>(client =>
{
    var emergencyUrl = builder.Configuration["Services:EmergencyServiceUrl"] ?? "http://localhost:5050";
    client.BaseAddress = new Uri(emergencyUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddSingleton<IForwardingService, InMemoryForwardingService>();
// Shared error format for all endpoints (Security Service API Details, section 1)
builder.Services.AddSharedErrorFormat();

// Register Birendra's View 9-1-1 Calls Service (Sprint 2)
builder.Services.AddCallViewing();

// OpenAPI/Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "Healthy"
    });
});

app.MapControllers();

app.Run();

public partial class Program { }
