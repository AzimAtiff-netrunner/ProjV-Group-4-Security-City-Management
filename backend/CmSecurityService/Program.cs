using CmSecurityService.Data;
using CmSecurityService.Forwarding;
using CmSecurityService.Calls;
using CmSecurityService.Common;
using CmSecurityService.Incidents;
using CmSecurityService.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CmSecurityService.Models;
using Microsoft.Extensions.Options;
using System.Security.Principal;
using CmSecurityService.Authorization;
using Microsoft.OpenApi.Models;

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

// Register vehicle list and telemetry service (Police Vehicle Telemetry)
builder.Services.AddVehicleTelemetry();

// OpenAPI/Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Security Service API", 
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter you JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is missing");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(Options =>
{
    Options.MapInboundClaims = false; 
    Options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SecurityAdministrator", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new AdministratorRequirement());
    });

    // Vehicle telemetry: any active officer may read, only Security Administrators may record.
    options.AddPolicy(TelemetryPolicies.Viewer, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new OfficerRoleRequirement(TelemetryPolicies.ViewerRoles));
    });

    options.AddPolicy(TelemetryPolicies.Writer, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new OfficerRoleRequirement(TelemetryPolicies.WriterRoles));
    });

    options.AddPolicy(TelemetryPolicies.Manager, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new OfficerRoleRequirement(TelemetryPolicies.ManagerRoles));
    });
});

builder.Services.AddScoped<IAuthorizationHandler, SecurityAdministratorHandler>();
builder.Services.AddScoped<IAuthorizationHandler, OfficerRoleHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
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
