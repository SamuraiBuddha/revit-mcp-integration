using System;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Serilog;
using Serilog.Events;
using Swan.Logging;
using RevitMcpServer.Controllers;

namespace RevitMcpServer
{
    public class RevitMcpServerApp : IExternalApplication
    {
        private static WebServer _webServer;
        private static Application _revitApp;
        private static UIApplication _uiApp;
        private static CancellationTokenSource _cancellationTokenSource;
        private static RevitApiWrapper _revitApiWrapper;
        private static Serilog.ILogger _logger;

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
            _revitApiWrapper = new RevitApiWrapper(_revitApp, _uiApp);
            
            Log.Information("RevitMcpServer starting up");
            Log.Information($"Revit Version: {_revitApp.VersionNumber}");
            
            // Start the MCP server
            Task.Run(() => StartMcpServer());
            
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                Log.Information("RevitMcpServer shutting down");
                
                // Stop the web server
                _cancellationTokenSource?.Cancel();
                _webServer?.Dispose();
                
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
                
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Logger = _logger;

            // Configure EmbedIO to use Serilog
            Swan.Logging.Logger.UnregisterLogger<ConsoleLogger>();
            Swan.Logging.Logger.RegisterLogger(new SerilogLogger());
        }
        
        private void StartMcpServer()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Create the web server
                _webServer = CreateWebServer("http://localhost:7891/");
                
                Log.Information("Starting MCP server on http://localhost:7891/");
                
                // Start the server
                _webServer.RunAsync(_cancellationTokenSource.Token).Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error starting MCP server");
            }
        }

        private WebServer CreateWebServer(string url)
        {
            var server = new WebServer(o => o
                                .WithUrlPrefix(url)
                                .WithMode(HttpListenerMode.EmbedIO))
                                .WithLocalSessionManager()
                                .WithCors()
                                .WithWebApi("/api", m => m
                                    .WithController(() => new BasicMcpController(_revitApp, _uiApp))
                                    .WithController(() => new ElementController(_revitApiWrapper, _logger)));

            return server;
        }
        
        // Access to Revit application for external classes
        public UIApplication UIApplication => _uiApp;
        public Application RevitApplication => _revitApp;
    }

    // Serilog adapter for EmbedIO
    public class SerilogLogger : Swan.Logging.ILogger
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        public void Dispose() { }

        public void Log(LogMessageReceivedEventArgs logEvent)
        {
            switch (logEvent.MessageType)
            {
                case LogLevel.Fatal:
                    Serilog.Log.Fatal(logEvent.Exception, logEvent.Message);
                    break;
                case LogLevel.Error:
                    Serilog.Log.Error(logEvent.Exception, logEvent.Message);
                    break;
                case LogLevel.Warning:
                    Serilog.Log.Warning(logEvent.Message);
                    break;
                case LogLevel.Info:
                    Serilog.Log.Information(logEvent.Message);
                    break;
                case LogLevel.Debug:
                    Serilog.Log.Debug(logEvent.Message);
                    break;
                case LogLevel.Trace:
                    Serilog.Log.Verbose(logEvent.Message);
                    break;
            }
        }
    }

    // Basic controller for MCP operations
    [Route("/api")]
    public class BasicMcpController : WebApiController
    {
        private readonly Application _revitApp;
        private readonly UIApplication _uiApp;

        public BasicMcpController(Application revitApp, UIApplication uiApp)
        {
            _revitApp = revitApp;
            _uiApp = uiApp;
        }

        [Route(HttpVerbs.Get, "/health")]
        public object GetHealth()
        {
            return new
            {
                status = "ok",
                timestamp = DateTime.UtcNow,
                service = "RevitMcpServer"
            };
        }

        [Route(HttpVerbs.Get, "/revit/version")]
        public object GetRevitVersion()
        {
            return new
            {
                versionNumber = _revitApp.VersionNumber,
                versionName = _revitApp.VersionName,
                versionBuild = _revitApp.VersionBuild,
                subVersionNumber = _revitApp.SubVersionNumber,
                language = _revitApp.Language.ToString()
            };
        }
    }
}
