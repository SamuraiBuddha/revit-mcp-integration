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
using Microsoft.Extensions.Logging;

namespace RevitMcpServer
{
    public class RevitMcpServerApp : IExternalApplication
    {
        private static WebServer _webServer;
        private static ControlledApplication _controlledApp;
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
            
            // Store Revit application reference (ControlledApplication for now)
            _controlledApp = application.ControlledApplication;
            
            Log.Information("RevitMcpServer starting up");
            Log.Information($"Revit Version: {_controlledApp.VersionNumber}");
            
            // Subscribe to application initialized event to get full Application access
            application.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;
            
            return Result.Succeeded;
        }

        private void OnApplicationInitialized(object sender, Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs e)
        {
            try
            {
                // Now we can get the full Application and UIApplication
                var app = sender as Application;
                if (app != null)
                {
                    var uiApp = new UIApplication(app);
                    
                    // Create logger adapter for Microsoft.Extensions.Logging
                    var loggerFactory = new SerilogLoggerFactory(_logger);
                    var msLogger = loggerFactory.CreateLogger<RevitApiWrapper>();
                    
                    _revitApiWrapper = new RevitApiWrapper(app, uiApp, msLogger);
                    
                    // Start the MCP server
                    Task.Run(() => StartMcpServer(app, uiApp));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing RevitMcpServer");
            }
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
        
        private void StartMcpServer(Application app, UIApplication uiApp)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Create the web server
                _webServer = CreateWebServer("http://localhost:7891/", app, uiApp);
                
                Log.Information("Starting MCP server on http://localhost:7891/");
                
                // Start the server
                _webServer.RunAsync(_cancellationTokenSource.Token).Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error starting MCP server");
            }
        }

        private WebServer CreateWebServer(string url, Application app, UIApplication uiApp)
        {
            var server = new WebServer(o => o
                                                .WithUrlPrefix(url)
                                                .WithMode(HttpListenerMode.EmbedIO))
                                                .WithLocalSessionManager()
                                                .WithCors()
                                                .WithWebApi("/api", m => m
                                                    .WithController(() => new BasicMcpController(app, uiApp))
                                                    .WithController(() => new ElementController(_revitApiWrapper, _logger)));

            return server;
        }
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

    // Serilog adapter for Microsoft.Extensions.Logging
    public class SerilogLoggerFactory : ILoggerFactory
    {
        private readonly Serilog.ILogger _logger;

        public SerilogLoggerFactory(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName)
        {
            return new SerilogMicrosoftLogger(_logger.ForContext("SourceContext", categoryName));
        }

        public void Dispose() { }
    }

    public class SerilogMicrosoftLogger : Microsoft.Extensions.Logging.ILogger
    {
        private readonly Serilog.ILogger _logger;

        public SerilogMicrosoftLogger(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, 
            Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null) return;

            var message = formatter(state, exception);

            switch (logLevel)
            {
                case Microsoft.Extensions.Logging.LogLevel.Trace:
                    _logger.Verbose(exception, message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Debug:
                    _logger.Debug(exception, message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Information:
                    _logger.Information(exception, message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Warning:
                    _logger.Warning(exception, message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Error:
                    _logger.Error(exception, message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Critical:
                    _logger.Fatal(exception, message);
                    break;
            }
        }
    }

    // Basic controller for MCP operations
    public class BasicMcpController : WebApiController
    {
        private readonly Application _revitApp;
        private readonly UIApplication _uiApp;

        public BasicMcpController(Application revitApp, UIApplication uiApp)
        {
            _revitApp = revitApp;
            _uiApp = uiApp;
        }

        [Route(HttpVerbs.Get, "health")]
        public object GetHealth()
        {
            return new
            {
                status = "ok",
                timestamp = DateTime.UtcNow,
                service = "RevitMcpServer"
            };
        }

        [Route(HttpVerbs.Get, "revit/version")]
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
