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

namespace RevitMcpServer
{
    public class RevitMcpServerApp : IExternalApplication
    {
        private static WebServer _webServer;
        private static Application _revitApp;
        private static UIApplication _uiApp;
        private static CancellationTokenSource _cancellationTokenSource;

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
                
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

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
                _webServer = CreateWebServer("http://localhost:5000/");
                
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
                        .WithController(() => new McpController(_revitApp, _uiApp))
                        .WithController(() => new ScanToBIMController(_revitApp, _uiApp))
                        .WithController(() => new UndergroundUtilitiesController(_revitApp, _uiApp)));

            return server;
        }
        
        // Access to Revit application for external classes
        public UIApplication UIApplication => _uiApp;
        public Application RevitApplication => _revitApp;
    }

    // Serilog adapter for EmbedIO
    public class SerilogLogger : ILogger
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

    // Base controller for MCP operations
    [Route("/mcp")]
    public class McpController : WebApiController
    {
        private readonly Application _revitApp;
        private readonly UIApplication _uiApp;

        public McpController(Application revitApp, UIApplication uiApp)
        {
            _revitApp = revitApp;
            _uiApp = uiApp;
        }

        [Route(HttpVerbs.Get, "/info")]
        public object GetInfo()
        {
            return new
            {
                status = "running",
                version = "1.0.0",
                revitVersion = _revitApp.VersionNumber
            };
        }

        [Route(HttpVerbs.Post, "/execute")]
        public async Task<object> Execute()
        {
            try
            {
                // In EmbedIO, use GetRequestBodyAsStringAsync() directly on the controller
                var requestBody = await GetRequestBodyAsStringAsync();
                // Process MCP request
                return new { success = true, message = "Command executed" };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
    }
}
