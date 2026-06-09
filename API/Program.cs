using System;
using System.Linq;
using API.RateLimiting;
using Application.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.DependencyInjection;
using Persistence.Middleware;
using Persistence.Repositories.Authentication;
using Persistence.Seeding;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog ----------
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
Log.Information("Application starting up");

// ---------- Services ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddApiRateLimiting();

// Trust the hosting proxy's X-Forwarded-* headers so the real client IP (not the proxy's) drives
// rate limiting and audit logging. KnownProxies/Networks are cleared because the proxy IP is not
// fixed on managed hosts (e.g. Render); the platform is the only hop in front of the app.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy => policy
        .WithOrigins("http://localhost:5173", "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// ---------- Middleware ----------
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ---------- Startup data ----------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    if (app.Environment.IsDevelopment())
        await services.GetRequiredService<FifeNDbContext>().Database.MigrateAsync();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await RoleSeeder.SeedAsync(roleManager);

    // Full demo dataset (founder/admins, categories, pilot vendors, listings, interactions, reviews):
    // Development by default, Production only with an explicit --seed flag. Idempotent.
    if (app.Environment.IsDevelopment() || args.Contains("--seed"))
        await DbSeeder.SeedAsync(services);
}

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Error(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
