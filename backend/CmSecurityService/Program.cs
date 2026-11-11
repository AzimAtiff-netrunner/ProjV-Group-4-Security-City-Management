using CmSecurityService.Calls;
using CmSecurityService.Incidents;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register Birendra's View Incidents Service (#216)
builder.Services.AddIncidentViewing();

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