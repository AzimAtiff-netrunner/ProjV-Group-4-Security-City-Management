using CmSecurityService.Data;
using CmSecurityService.Incidents;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Database (MySQL via Pomelo)
var cs = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// Register Birendra's View Incidents Service (#216)
builder.Services.AddIncidentViewing();

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

app.MapControllers();

app.Run();
