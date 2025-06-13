using System;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace RevitMcpServer
{
    public class RevitMcpServerApp : IExternalApplication
    {
        private static IWebHost _webHost;
        private static Application _revitApp;
        private static UIApplication _uiApp;
        private static ILogger<RevitMcpServerApp> _logger;

        // Singleton instance for accessing throughout the application
        public static RevitMcpServerApp Instance { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            Instance = this;
            
            // Set up logging
            ConfigureLogging();
            
            // Store Revit application references
            _uiApp = new UIApplication(application.ControlledApplication);
            _revitApp = _uiApp.Application;
            
            // Start the MCP server
            Task.Run(() => StartMcpServer());
            
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                // Stop the web server
                _webHost?.StopAsync().Wait();
                _webHost?.Dispose();
                
                Log.CloseAndFlush();
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error shutting down MCP server: {ex.Message}");
                return Result.Failed;
            }
        }

        private void ConfigureLogging()
        {
            string logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RevitMcpServer", "logs", "revit-mcp-.log");
                
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
        
        private void StartMcpServer()
        {
            try
            {
                _webHost = WebHost.CreateDefaultBuilder()
                    .UseStartup<Startup>()
                    .UseUrls("http://localhost:5000")
                    .ConfigureServices(services =>
                    {
                        // Register Revit services
                        services.AddSingleton(_revitApp);
                        services.AddSingleton(_uiApp);
                        services.AddSingleton<RevitApiWrapper>();
                    })
                    .UseSerilog()
                    .Build();
                    
                _webHost.Run();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error starting MCP server");
            }
        }
        
        // Access to Revit application for external classes
        public UIApplication UIApplication => _uiApp;
        public Application RevitApplication => _revitApp;
    }
    
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });
                
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseCors("AllowAll");
            
            app.UseRouting();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            // Add MCP middleware
            app.UseMiddleware<McpMiddleware>();
        }
    }
    
    // Simple middleware to handle MCP requests
    public class McpMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<McpMiddleware> _logger;
        
        public McpMiddleware(RequestDelegate next, ILogger<McpMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        
        public async Task Invoke(HttpContext context)
        {
            // Log all requests
            _logger.LogInformation($"MCP Request: {context.Request.Method} {context.Request.Path}");
            
            // Continue with the pipeline
            await _next(context);
        }
    }
}