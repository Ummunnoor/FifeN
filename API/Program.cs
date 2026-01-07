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
var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();
Log.Information("Application Starting Up");
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
builder.Services.AddDbContext<FifeNDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddApplicationServices();

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

try
{
    var app = builder.Build();
    app.UseCors("AllowFrontend");
    app.UseSerilogRequestLogging();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    // Configure the HTTP request pipeline.
    app.UseAuthorization();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.MapControllers();

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