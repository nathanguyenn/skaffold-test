using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MicroserviceExample.Data;
using MicroserviceExample.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Local-only overrides (e.g. dev connection string). Gitignored and absent from the
// container image, so it's optional — cluster config comes from the env/secret instead.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Readiness probe touches the DB; liveness does not (see endpoints below).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db", tags: ["ready"]);

builder.Services.AddOpenApi();

var app = builder.Build();

// One-shot migration mode: `dotnet MicroserviceExample.dll migrate` (used by the
// Deployment's init container). Applies EF Core migrations, then exits.
if (args.Contains("migrate"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    return;
}

// OpenAPI document + interactive Swagger UI. Enabled in all environments for this
// lab; gate behind app.Environment.IsDevelopment() (or auth) for real production.
app.MapOpenApi();                    // OpenAPI JSON at /openapi/v1.json
app.UseSwaggerUI(o =>                 // Swagger UI at /swagger
{
    o.SwaggerEndpoint("/openapi/v1.json", "microservice-example v1");
    o.DocumentTitle = "microservice-example API";
});

// Liveness: process is up. No dependencies checked (Predicate excludes all checks).
app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });

// Readiness: only checks tagged "ready" (DB connectivity).
app.MapHealthChecks("/readyz", new HealthCheckOptions
{
    Predicate = hc => hc.Tags.Contains("ready")
});

app.MapProjectEndpoints();

app.Run();

// Exposed for WebApplicationFactory<Program> in the test project.
public partial class Program;
