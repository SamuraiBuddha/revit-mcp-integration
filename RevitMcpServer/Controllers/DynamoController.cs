using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RevitMcpServer.Models;

namespace RevitMcpServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DynamoController : ControllerBase
    {
        private readonly RevitApiWrapper _revitApi;
        private readonly ILogger<DynamoController> _logger;

        public DynamoController(RevitApiWrapper revitApi, ILogger<DynamoController> logger)
        {
            _revitApi = revitApi;
            _logger = logger;
        }

        /// <summary>
        /// Run a Dynamo script from a file path
        /// </summary>
        [HttpPost("run")]
        public ActionResult RunDynamoScript([FromBody] RunScriptRequest request)
        {
            try
            {
                _logger.LogInformation($"Running Dynamo script: {request.ScriptPath}");
                
                // Verify file exists
                if (!System.IO.File.Exists(request.ScriptPath))
                {
                    return NotFound(new { error = $"Script not found: {request.ScriptPath}" });
                }
                
                // Execute Dynamo script - in a real implementation, this would call into the Dynamo API
                // This is a placeholder implementation
                var result = ExecuteDynamoScript(request.ScriptPath, request.Parameters);
                
                return Ok(new { success = true, result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error running Dynamo script: {request.ScriptPath}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        /// <summary>
        /// List available Dynamo scripts in a directory
        /// </summary>
        [HttpGet("list")]
        public ActionResult<IEnumerable<string>> ListScripts([FromQuery] string directory = null)
        {
            try
            {
                // Default to Documents/Dynamo/Scripts if not specified
                string scriptsDir = directory ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Dynamo", "Scripts");
                    
                _logger.LogInformation($"Listing Dynamo scripts in: {scriptsDir}");
                
                if (!Directory.Exists(scriptsDir))
                {
                    return NotFound(new { error = $"Directory not found: {scriptsDir}" });
                }
                
                var scripts = Directory.GetFiles(scriptsDir, "*.dyn")
                    .Select(Path.GetFileName)
                    .ToList();
                    
                return Ok(scripts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing Dynamo scripts");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        /// <summary>
        /// MCP endpoint for Dynamo operations
        /// </summary>
        [HttpPost("mcp")]
        public ActionResult<McpResponse> ProcessMcpRequest([FromBody] DynamoRequest request)
        {
            try
            {
                _logger.LogInformation($"Processing MCP request: {request.Action}");
                
                switch (request.Action)
                {
                    case "runDynamoScript":
                        if (request.Parameters.TryGetValue("scriptPath", out var scriptPathObj) && 
                            scriptPathObj is string scriptPath)
                        {
                            // Extract parameters from the request
                            Dictionary<string, object> scriptParams = new Dictionary<string, object>();
                            if (request.Parameters.TryGetValue("scriptParameters", out var paramsObj) && 
                                paramsObj is Dictionary<string, object> paramDict)
                            {
                                scriptParams = paramDict;
                            }
                            
                            if (!System.IO.File.Exists(scriptPath))
                            {
                                return NotFound(McpResponse.Error($"Script not found: {scriptPath}"));
                            }
                            
                            var result = ExecuteDynamoScript(scriptPath, scriptParams);
                            return Ok(McpResponse.Success(result));
                        }
                        return BadRequest(McpResponse.Error("Missing or invalid scriptPath parameter"));
                        
                    case "listDynamoScripts":
                        string directory = null;
                        if (request.Parameters.TryGetValue("directory", out var dirObj) && dirObj is string dir)
                        {
                            directory = dir;
                        }
                        
                        // Default to Documents/Dynamo/Scripts if not specified
                        string scriptsDir = directory ?? Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            "Dynamo", "Scripts");
                            
                        if (!Directory.Exists(scriptsDir))
                        {
                            return NotFound(McpResponse.Error($"Directory not found: {scriptsDir}"));
                        }
                        
                        var scripts = Directory.GetFiles(scriptsDir, "*.dyn")
                            .Select(Path.GetFileName)
                            .ToList();
                            
                        return Ok(McpResponse.Success(scripts));
                        
                    default:
                        return BadRequest(McpResponse.Error($"Unknown action: {request.Action}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing MCP request: {request.Action}");
                return StatusCode(500, McpResponse.Error(ex.Message));
            }
        }
        
        /// <summary>
        /// Placeholder for executing a Dynamo script
        /// In a real implementation, this would use the Dynamo API
        /// </summary>
        private object ExecuteDynamoScript(string scriptPath, Dictionary<string, object> parameters)
        {
            _logger.LogInformation($"Executing Dynamo script: {scriptPath} with parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");
            
            // Placeholder implementation - in a real world scenario, this would use the Dynamo API
            // For example, using the DynamoRevit namespace to run scripts
            
            // Mock result - in a real implementation, this would be the actual output from Dynamo
            return new
            {
                executed = true,
                scriptPath = scriptPath,
                parameters = parameters,
                timestamp = DateTime.Now,
                results = new[] 
                {
                    new { name = "Output1", value = "Sample output value" },
                    new { name = "Output2", value = 42 }
                }
            };
        }
    }
    
    /// <summary>
    /// Request model for running a Dynamo script
    /// </summary>
    public class RunScriptRequest
    {
        public string ScriptPath { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}