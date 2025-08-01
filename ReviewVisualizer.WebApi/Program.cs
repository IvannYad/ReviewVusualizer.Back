using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data.Mapper;
using ReviewVisualizer.Data;
using ReviewVisualizer.WebApi.RatingCalculationEngine;
using ReviewVisualizer.WebApi;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Events;
using ReviewVisualizer.WebApi.Processor;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using ReviewVisualizer.AuthLibrary;
using Microsoft.AspNetCore.Authentication;
using ReviewVisualizer.WebApi.Services;

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
        policy.WithOrigins("http://localhost:3000") // Your React app URL
            .AllowAnyHeader()
            .AllowAnyMethod()
           .SetPreflightMaxAge(TimeSpan.FromSeconds(10))
           .AllowCredentials();
    });
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
}, ServiceLifetime.Singleton);
builder.Services.AddSingleton<IQueueController, QueueController>();
builder.Services.AddSingleton<IRatingCalculatingEngine, RatingCalculatingEngine>();
builder.Services.AddSingleton<IProcessorHost, ProcessorHost>();
builder.Services.AddAutoMapper(typeof(MyMapper));

builder.Services.AddScoped(_ => new PasswordService(builder.Configuration["PasswordSecret"] ?? string.Empty));
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/auth/login";
        options.AccessDeniedPath = "/auth/access-denied";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.UseCookiePolicy(new CookiePolicyOptions()
{
    MinimumSameSitePolicy = SameSiteMode.None,
    HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.Always,
});

app.UseSwagger();

// Enable middleware to serve Swagger UI (HTML, JS, CSS, etc.).
app.UseSwaggerUI();

app.MapControllers();
app.StartProcessorHost();
app.StartRatingCalculationEngine();
app.Run();
