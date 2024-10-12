using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data.Mapper;
using ReviewVisualizer.Data;
using ReviewVisualizer.WebApi.RatingCalculationEngine;
using ReviewVisualizer.WebApi;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Events;

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

builder.Services.AddSingleton<IRatingCalculatingEngine, RatingCalculatingEngine>();

builder.Services.AddAutoMapper(typeof(MyMapper));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.StartRatingCalculationEngine();
app.Run();
