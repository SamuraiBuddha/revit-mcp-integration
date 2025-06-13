using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RevitMcpServer.Models;

namespace RevitMcpServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElementController : ControllerBase
    {
        private readonly RevitApiWrapper _revitApi;
        private readonly ILogger<ElementController> _logger;

        public ElementController(RevitApiWrapper revitApi, ILogger<ElementController> logger)
        {
            _revitApi = revitApi;
            _logger = logger;
        }

        /// <summary>
        /// Get elements by category
        /// </summary>
        [HttpGet("category/{categoryName}")]
        public ActionResult<IEnumerable<ElementModel>> GetByCategory(string categoryName)
        {
            try
            {
                _logger.LogInformation($"Getting elements by category: {categoryName}");
                var elements = _revitApi.GetElementsByCategory(categoryName);
                return Ok(elements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting elements by category: {categoryName}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get elements by type
        /// </summary>
        [HttpGet("type/{typeName}")]
        public ActionResult<IEnumerable<ElementModel>> GetByType(string typeName)
        {
            try
            {
                _logger.LogInformation($"Getting elements by type: {typeName}");
                var elements = _revitApi.GetElementsByType(typeName);
                return Ok(elements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting elements by type: {typeName}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get element by ID
        /// </summary>
        [HttpGet("{elementId}")]
        public ActionResult<ElementModel> GetById(int elementId)
        {
            try
            {
                _logger.LogInformation($"Getting element by ID: {elementId}");
                // Example implementation - would need to add a GetElementById method to RevitApiWrapper
                throw new NotImplementedException("GetElementById not implemented yet");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting element by ID: {elementId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Modify element parameter
        /// </summary>
        [HttpPut("{elementId}/parameter")]
        public ActionResult ModifyParameter(int elementId, [FromBody] ParameterUpdateModel model)
        {
            try
            {
                _logger.LogInformation($"Modifying parameter {model.ParameterName} for element {elementId}");
                var success = _revitApi.ModifyElementParameter(elementId, model.ParameterName, model.Value);
                
                if (success)
                {
                    return Ok(new { success = true, message = "Parameter updated successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to update parameter" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error modifying parameter for element {elementId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// MCP endpoint for element operations
        /// </summary>
        [HttpPost("mcp")]
        public ActionResult<McpResponse> ProcessMcpRequest([FromBody] ElementRequest request)
        {
            try
            {
                _logger.LogInformation($"Processing MCP request: {request.Action}");
                
                switch (request.Action)
                {
                    case "getElementsByCategory":
                        if (request.Parameters.TryGetValue("category", out var categoryObj) && categoryObj is string category)
                        {
                            var elements = _revitApi.GetElementsByCategory(category);
                            return Ok(McpResponse.Success(elements));
                        }
                        return BadRequest(McpResponse.Error("Missing or invalid category parameter"));
                        
                    case "getElementsByType":
                        if (request.Parameters.TryGetValue("type", out var typeObj) && typeObj is string type)
                        {
                            var elements = _revitApi.GetElementsByType(type);
                            return Ok(McpResponse.Success(elements));
                        }
                        return BadRequest(McpResponse.Error("Missing or invalid type parameter"));
                        
                    case "modifyElementParameter":
                        if (request.Parameters.TryGetValue("elementId", out var idObj) && 
                            request.Parameters.TryGetValue("parameterName", out var nameObj) &&
                            request.Parameters.TryGetValue("value", out var valueObj))
                        {
                            int elementId = Convert.ToInt32(idObj);
                            string paramName = nameObj.ToString();
                            string value = valueObj.ToString();
                            
                            var success = _revitApi.ModifyElementParameter(elementId, paramName, value);
                            return Ok(McpResponse.Success(new { success }));
                        }
                        return BadRequest(McpResponse.Error("Missing or invalid parameters"));
                        
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
    }

    /// <summary>
    /// Model for parameter update requests
    /// </summary>
    public class ParameterUpdateModel
    {
        public string ParameterName { get; set; }
        public string Value { get; set; }
    }
}