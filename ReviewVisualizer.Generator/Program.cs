using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
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
            var cookieSettings = builder.Configuration
                .GetSection("AuthCookieSettings")
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
                    policy.WithOrigins(corsSettings.AllowedOrigins)
                        .WithHeaders(corsSettings.AllowedHeaders)
                        .WithMethods(corsSettings.AllowedMethods)
                        .SetPreflightMaxAge(TimeSpan.FromSeconds(corsSettings.PreflightMaxAgeInSeconds))
                        .AllowCredentials();
                });
            });
                
            builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(@"C:\Users\iyadc\OneDrive - SoftServe, Inc\Desktop\it\PeEx\Middle\ReviewVisualizer\persist-keys"))
            .SetApplicationName("ReviewerVisualizer");
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = cookieSettings!.Name;
                options.Cookie.Path = cookieSettings!.Path;
                options.Cookie.SameSite = cookieSettings!.SameSite;
                options.Cookie.SecurePolicy = cookieSettings!.Secure;
                options.Cookie.HttpOnly = cookieSettings!.HttpOnly.HasFlag(HttpOnlyPolicy.Always);
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

            // Global exception handling.
            if (app.Environment.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler();
            app.UseStatusCodePages();


            // Security middlewares.
            app.UseHttpsRedirection();
            app.UseCors();
            app.UseCookiePolicy(new CookiePolicyOptions()
            {
                MinimumSameSitePolicy = cookieSettings!.SameSite,
                HttpOnly = cookieSettings!.HttpOnly,
                Secure = cookieSettings!.Secure,
            });
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.UseHangfireDashboard(options: new DashboardOptions
            {
                DashboardTitle = "Reviews processing dashboard"
            });
            app.MapHangfireDashboard();

            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapHealthChecks("/");

            app.Run();
        }
    }
}