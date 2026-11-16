using API.Hubs;
using System.Text.Json.Serialization;
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
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog ----------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
Log.Information("Application starting up");

// ---------- Services ----------
builder.Services.AddControllers()
    // Serialize enums as their string names (e.g. "Lagos", "Fixed") rather than integers, so the
    // JSON contract matches the documented API (BACKEND_SPEC / FRONTEND_PDR) and typed clients.
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddApiRateLimiting();

// Trust forwarded headers only from explicitly configured proxy addresses. With no configured
// proxy, the framework ignores forwarded headers and uses the direct peer address.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        if (System.Net.IPAddress.TryParse(proxy, out var address))
            options.KnownProxies.Add(address);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy => policy
    .WithOrigins("http://localhost:5173")
    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
    .WithHeaders("Authorization", "Content-Type")
        .AllowCredentials());
});

var app = builder.Build();

// ---------- Middleware ----------
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chats");

// ---------- Startup data ----------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    if (app.Environment.IsDevelopment())
        await services.GetRequiredService<FifeNDbContext>().Database.MigrateAsync();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await RoleSeeder.SeedAsync(roleManager);
    await DbSeeder.EnsureBusinessAdminAsync(services);

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
