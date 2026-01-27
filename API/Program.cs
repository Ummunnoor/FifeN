using Application.Validators;
using Application.Validators.Product;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.DependencyInjection;
using Application.DependencyInjection;
using Serilog;
using Persistence.Middleware;
using Microsoft.AspNetCore.Identity;
using Persistence.Repositories.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog ----------
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
Log.Information("Application Starting Up");

// ---------- Services ----------

// Controllers
builder.Services.AddControllers();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
builder.Services.AddScoped<IValidationService, ValidationService>();

// DbContext
builder.Services.AddDbContext<FifeNDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Application & Persistence Services
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddApplicationServices();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",   // Vite
                "http://localhost:3000"    // React
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ---------- Middleware ----------

// Exception Handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// CORS
app.UseCors("AllowFrontend");

// Serilog Request Logging
app.UseSerilogRequestLogging();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Authentication / Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// ---------- Seed Roles Safely ----------

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    await RoleSeeder.SeedAsync(roleManager);
}

// ---------- Run Application ----------
try
{
    app.Run();
}
catch (System.Exception ex)
{
    Log.Logger.Error(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
