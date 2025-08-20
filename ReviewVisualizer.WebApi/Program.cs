using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data.Mapper;
using ReviewVisualizer.Data;
using ReviewVisualizer.WebApi;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Authentication.Cookies;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.WebApi.Services;
using ReviewVisualizer.AuthLibrary.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.CookiePolicy;
using ReviewVisualizer.Data.Enums;

namespace VisualizerProject
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var cookieSettings = builder.Configuration
                .GetSection("AuthCookieSettings")
                .Get<CookieSettings>();

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
                    policy.WithOrigins("https://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetPreflightMaxAge(TimeSpan.FromSeconds(10))
                        .AllowCredentials();
                });
            });

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            }, ServiceLifetime.Scoped);
            if (builder.Environment.IsDevelopment())
                builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            //builder.Services.AddSingleton<IRatingCalculatingEngine, RatingCalculatingEngine>();
            //builder.Services.AddSingleton<IProcessorHost, ProcessorHost>();
            builder.Services.AddAutoMapper(typeof(MyMapper));

            builder.Services.AddScoped(_ => new PasswordService(builder.Configuration["PasswordSecret"] ?? string.Empty));
            builder.Services.AddScoped<IAuthService, AuthService>();

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

            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = (ctx) => builder.Environment.IsDevelopment();
            });

            var app = builder.Build();

            // Global exception handling.
            if (app.Environment.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler();
            app.UseStatusCodePages();

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

            app.UseSwagger();
            app.UseSwaggerUI();

            app.MapControllers();

            app.StartProcessorHost();
            app.StartRatingCalculationEngine();

            var mvcOpts = app.Services.GetRequiredService<IOptions<MvcOptions>>();
            foreach (var f in mvcOpts.Value.OutputFormatters)
            {
                Console.WriteLine(f.GetType().FullName);
            }

            app.Run();
        }
    }
}