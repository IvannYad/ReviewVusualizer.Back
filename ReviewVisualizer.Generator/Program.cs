using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Mapper;
using ReviewVisualizer.Generator;
using ReviewVisualizer.Generator.Generator;
using Serilog;
using System.Text.Json.Serialization;
using Serilog.Events;
using Hangfire;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .MinimumLevel.Override("System", LogEventLevel.Error)
    .ReadFrom.Configuration(builder.Configuration) // Read config from appsettings.json
    .CreateLogger();

builder.Host.UseSerilog();
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Register Autofac-specific container.
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register ApplicationDbContext manually with configuration.
    containerBuilder.Register(c =>
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        return new ApplicationDbContext(optionsBuilder.Options);
    }).AsSelf();

    // Register custom services.
    containerBuilder.RegisterType<QueueController>().As<IQueueController>().SingleInstance();
    containerBuilder.RegisterType<GeneratorHost>().As<IGeneratorHost>();

    // Register AutoMapper.
    var mapperConfiguration = new MapperConfiguration(cfg => { cfg.AddProfile(new MyMapper()); });
    var mapper = mapperConfiguration.CreateMapper();
    containerBuilder.RegisterInstance(mapper).SingleInstance();

    containerBuilder.RegisterGeneric(typeof(Logger<>))
                    .As(typeof(ILogger<>))
                    .SingleInstance();
});


builder.Services.AddControllers().AddJsonOptions(ops =>
{
    ops.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

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

builder.Services.AddHangfireServices(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.UseHangfireDashboard(options: new DashboardOptions
{
    DashboardTitle = "Reviews processing dashboard"
});
app.MapHangfireDashboard();

app.Run();
