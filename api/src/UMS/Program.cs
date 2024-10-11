using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using UMS.Application;
using UMS.Infrastracture;
using UMS.Infrastructure.Data;

var logger = Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo
    .Console().CreateLogger();
logger.Information("Starting web host");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((_, config) => config.ReadFrom.Configuration(builder.Configuration));
var mcrLogger = new SerilogLoggerFactory(logger).CreateLogger<Program>();

builder.Services.AddSerilog(options =>
{
    options.MinimumLevel.Information()
     .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
     .MinimumLevel.Override("System", LogEventLevel.Warning)
     .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information);
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Register the AppDbContext to the DI container.

/* keyVaultUrl = new Uri(builder.Configuration.GetSection("KeyVaultURL").Value!);
var azureCredential = new DefaultAzureCredential();
builder.Configuration.AddAzureKeyVault(keyVaultUrl, azureCredential);
var cns = builder.Configuration.GetSection("azur-sql-ums").Value;*/

var cns = builder.Configuration.GetConnectionString("cns");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(cns));

// Register Infrastructure-level services to the DI container
builder.Services.AddInfrastructureServices(builder.Configuration, mcrLogger);

// Register Application-level services to the DI container
builder.Services.AddApplicationServices(builder.Configuration, mcrLogger);

// Register AutoMapper with the DI container
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsApi", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}
else
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UMS V1");
        c.RoutePrefix = string.Empty;
    });
}
app.UseCors("CorsApi");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
