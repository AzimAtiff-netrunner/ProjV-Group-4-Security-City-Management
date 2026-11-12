using CmSecurityService.Calls;
using CmSecurityService.Common;
using CmSecurityService.Forwarding;
using CmSecurityService.Incidents;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Shared error format for all endpoints
builder.Services.AddSharedErrorFormat();

// Register View Incidents Service (#216)
builder.Services.AddIncidentViewing();

// Register View 9-1-1 Calls Service (Sprint 2)
builder.Services.AddCallViewing();

// Register 9-1-1 Call Forwarding Client & Service (#219)
builder.Services.AddHttpClient<IEmergencyServiceClient, EmergencyServiceClient>(client =>
{
    var emergencyUrl = builder.Configuration["Services:EmergencyServiceUrl"] ?? "http://localhost:5050";
    client.BaseAddress = new Uri(emergencyUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddSingleton<IForwardingService, InMemoryForwardingService>();

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
