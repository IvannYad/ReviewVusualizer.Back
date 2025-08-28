using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.AuthLibrary.Extensions;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Mapper;
using ReviewVisualizer.WebApi;
using ReviewVisualizer.WebApi.Services;
using Serilog;
using Serilog.Events;
using System.Text.Json.Serialization;

namespace VisualizerProject
{
    public class Program
    {
        private const int SqlServerMaxRetriesOnFailureCount = 3;
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var authCookieSettings = builder.Configuration
                .GetSection("AuthCookieSettings")
                .Get<CookieSettings>();

            var cookiesSettings = builder.Configuration
                .GetSection("CookiesSettings")
                .Get<CookieSettings>();

            var corsSettings = builder.Configuration
                .GetSection("CorsSettings")
                .Get<CorsSettings>()!;

            // Add services to the container.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .MinimumLevel.Override("System", LogEventLevel.Error)
                .ReadFrom.Configuration(builder.Configuration) // Read config from appsettings.json
                .CreateLogger();

            builder.Host.UseSerilog();

            builder.Services.AddControllers(options =>
            {
                options.RespectBrowserAcceptHeader = false;
            }).AddJsonOptions(ops =>
            {
                ops.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            })
                .AddXmlDataContractSerializerFormatters();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen();

            builder.Services.AddCors(opt =>
            {
                opt.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(corsSettings.AllowedOrigins)
                        .WithHeaders(corsSettings.AllowedHeaders)
                        .WithMethods(corsSettings.AllowedMethods)
                        .SetPreflightMaxAge(TimeSpan.FromSeconds(corsSettings.PreflightMaxAgeInSeconds))
                        .AllowCredentials();
                });
            });

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), opts =>
                {
                    opts.EnableRetryOnFailure(SqlServerMaxRetriesOnFailureCount);
                });
            }, ServiceLifetime.Scoped);

            if (builder.Environment.IsDevelopment())
                builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddAutoMapper(typeof(MyMapper));

            builder.Services.AddScoped(_ => new ImageService(builder));
            builder.Services.AddScoped(_ => new PasswordService(builder.Configuration["PasswordSecret"] ?? string.Empty));
            builder.Services.AddScoped<IAuthService, AuthService>();

            builder.AddDataProtection();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Domain = authCookieSettings!.Domain;
                    options.Cookie.Name = authCookieSettings!.Name;
                    options.Cookie.Path = authCookieSettings!.Path;
                    options.Cookie.SameSite = authCookieSettings!.SameSite;
                    options.Cookie.SecurePolicy = authCookieSettings!.Secure;
                    options.Cookie.HttpOnly = authCookieSettings!.HttpOnly.HasFlag(HttpOnlyPolicy.Always);
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

            builder.Services.AddAuthorizationPolicies()
                .AddAuthorizationHandlers();

            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = (ctx) => builder.Environment.IsDevelopment();
            });

            builder.Services.AddHealthChecks();

            var app = builder.Build();

            // Global exception handling.
            if (app.Environment.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler();

            app.UseStatusCodePages();

            app.MapHealthChecks("/health");

            app.UseHttpsRedirection();
            app.UseCors();

            app.UseCookiePolicy(new CookiePolicyOptions()
            {
                MinimumSameSitePolicy = cookiesSettings!.SameSite,
                HttpOnly = cookiesSettings!.HttpOnly,
                Secure = cookiesSettings!.Secure
            });
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.MapControllers();

            app.StartProcessorHost();
            app.StartRatingCalculationEngine();

            app.AddAdminUser();

            app.Run();
        }
    }
}