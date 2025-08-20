using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Mapper;
using ReviewVisualizer.Generator.Generator;
using Serilog;
using System.Text.Json.Serialization;
using Serilog.Events;
using Hangfire;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using ReviewVisualizer.AuthLibrary.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using ReviewVisualizer.AuthLibrary;
using Microsoft.AspNetCore.CookiePolicy;
using ReviewVisualizer.Generator.Utils;
using ReviewVisualizer.Data.Enums;

namespace GeneratorProject
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var cookieSettings = builder.Configuration
                .GetSection("AuthCookieSettings")
                .Get<CookieSettings>();

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
                    optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
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
                    policy.WithOrigins("https://localhost:3000") // Your React app URL
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetPreflightMaxAge(TimeSpan.FromSeconds(10))
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

            app.Run();
        }
    }
}
