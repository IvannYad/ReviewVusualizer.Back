using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Mapper;
using ReviewVisualizer.Generator;
using ReviewVisualizer.Generator.Generator;
using Serilog;
using System.Text.Json.Serialization;
using Serilog.Events;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .MinimumLevel.Override("System", LogEventLevel.Error)
    .ReadFrom.Configuration(builder.Configuration) // Read config from appsettings.json
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddControllers().AddJsonOptions(ops =>
{
    ops.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("*")
           .WithHeaders("*")
           .WithMethods("*")
           .WithExposedHeaders("*")
           .SetPreflightMaxAge(TimeSpan.FromSeconds(10));
    });
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
}, ServiceLifetime.Singleton);

builder.Services.AddSingleton<IQueueController, QueueController>();

builder.Services.AddSingleton<IGeneratorHost, GeneratorHost>();
builder.Services.AddAutoMapper(typeof(MyMapper));

builder.Services.AddHangfireServices(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.UseHangfireDashboard(options: new DashboardOptions()
{
    DashboardTitle = "Reviews processing dashboard"
});
app.MapHangfireDashboard();

app.Run();
