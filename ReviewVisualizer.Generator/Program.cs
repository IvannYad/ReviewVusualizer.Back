using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.AuthLibrary.Extensions;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Mapper;
using ReviewVisualizer.Generator.Generator;
using ReviewVisualizer.Generator.Utils;
using Serilog;
using Serilog.Events;
using System.Text.Json.Serialization;

namespace GeneratorProject
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

            // Logging services.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .MinimumLevel.Override("System", LogEventLevel.Error)
                .ReadFrom.Configuration(builder.Configuration) // Read config from appsettings.json
                .CreateLogger();
            builder.Host.UseSerilog();
            
            // Log startup information
            Log.Information("Starting Generator service...");
            Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
            Log.Information("Application Name: {AppName}", builder.Configuration["AppName"]);

            // Add Autofac.
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            // Register Autofac-specific container.
            builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                // Register ApplicationDbContext manually with configuration.
                containerBuilder.Register(c =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), opts =>
                    {
                        opts.EnableRetryOnFailure(SqlServerMaxRetriesOnFailureCount);
                    });
                    return new ApplicationDbContext(optionsBuilder.Options);
                }).AsSelf();

                // Register custom services.
                containerBuilder.RegisterType<GeneratorHost>().As<IGeneratorHost>();

                // Register AutoMapper.
                var mapperConfiguration = new MapperConfiguration(cfg => { cfg.AddProfile(new MyMapper()); });
                var mapper = mapperConfiguration.CreateMapper();
                containerBuilder.RegisterInstance(mapper).SingleInstance();

                containerBuilder.RegisterGeneric(typeof(Logger<>))
                                .As(typeof(ILogger<>))
                                .SingleInstance();

                containerBuilder.RegisterType<HangfireProxy>().As<IHangfireProxy>();
            });


            builder.Services.AddControllers().AddJsonOptions(ops =>
            {
                ops.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
            builder.Services.AddEndpointsApiExplorer();

            // Swagger service.
            builder.Services.AddSwaggerGen();

            // Security services.
            builder.Services.AddCors(opt =>
            {
                opt.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(corsSettings.GetAllowedOriginsArray())
                        .WithHeaders(corsSettings.AllowedHeaders)
                        .WithMethods(corsSettings.AllowedMethods)
                        .SetPreflightMaxAge(TimeSpan.FromSeconds(corsSettings.PreflightMaxAgeInSeconds))
                        .AllowCredentials();
                });
            });

            builder.AddDataProtection();
            
            // Log Data Protection configuration
            Log.Information("Data Protection configured with AppName: {AppName}", builder.Configuration["AppName"]);
            Log.Information("Data Protection URL: {Url}", builder.Configuration["DataProtection:Url"]);
            Log.Information("Data Protection Container: {Container}", builder.Configuration["DataProtection:ContainerName"]);

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
                        Log.Warning("Authentication failed - redirecting to login for request: {Path}", context.Request.Path);
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    };
                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        Log.Warning("Access denied - redirecting for request: {Path}", context.Request.Path);
                        context.Response.StatusCode = 403;
                        return Task.CompletedTask;
                    };
                });
            
            // Log authentication configuration
            Log.Information("Authentication configured with Cookie Domain: {Domain}", authCookieSettings!.Domain);
            Log.Information("Authentication configured with Cookie Name: {Name}", authCookieSettings!.Name);
            builder.Services.AddAuthorizationPolicies()
                .AddAuthorizationHandlers();

            // Backgroung jobs processing services.
            builder.Services.AddHangfireServices(builder.Configuration);

            // Problem details for beautiful responces to user.
            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = (ctx) => builder.Environment.IsDevelopment();
            });

            builder.Services.Configure<RouteOptions>(options =>
            {
                options.ConstraintMap.Add("generatorType", typeof(GeneratorTypeRouteConstraint<GeneratorType>));
            });

            builder.Services.AddHealthChecks();

            var app = builder.Build();
            
            Log.Information("Application built successfully");

            // Global exception handling.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                Log.Information("Development exception page enabled");
            }
            else
            {
                app.UseExceptionHandler();
                Log.Information("Production exception handler enabled");
            }

            app.UseStatusCodePages();

            // Security middlewares.
            app.UseHttpsRedirection();
            app.UseCors();
            app.UseCookiePolicy(new CookiePolicyOptions()
            {
                MinimumSameSitePolicy = cookiesSettings!.SameSite,
                HttpOnly = cookiesSettings!.HttpOnly,
                Secure = cookiesSettings!.Secure,
            });
            app.UseAuthentication();
            Log.Information("Authentication middleware added");
            
            app.UseAuthorization();
            Log.Information("Authorization middleware added");

            app.MapHealthChecks("/health");
            app.MapControllers();

            app.UseHangfireDashboard(options: new DashboardOptions
            {
                DashboardTitle = "Reviews processing dashboard",
                Authorization = [new AllowAllDashboardAuthorizationFilter()],
            });
            app.MapHangfireDashboard();

            app.UseSwagger();
            app.UseSwaggerUI();

            Log.Information("Generator service starting up...");
            Log.Information("Swagger UI available at /swagger");
            Log.Information("Health check available at /health");
            
            app.Run();
        }
    }
}