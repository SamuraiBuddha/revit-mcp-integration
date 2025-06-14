using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Newtonsoft.Json;
using RevitMcpServer.Models;

namespace RevitMcpServer.Controllers
{
    public class ElementController : WebApiController
    {
        private readonly RevitApiWrapper _revitApi;
        private readonly Serilog.ILogger _logger;

        public ElementController(RevitApiWrapper revitApi, Serilog.ILogger logger)
        {
            _revitApi = revitApi;
            _logger = logger;
        }

        /// <summary>
        /// Get elements by category
        /// </summary>
        [Route(HttpVerbs.Get, "/api/element/category/{categoryName}")]
        public async Task<IEnumerable<ElementModel>> GetByCategory(string categoryName)
        {
            try
            {
                _logger.Information($"Getting elements by category: {categoryName}");
                var elements = _revitApi.GetElementsByCategory(categoryName);
                return elements;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error getting elements by category: {categoryName}");
                throw new HttpException(500, ex.Message);
            }
        }

        /// <summary>
        /// Get elements by type
        /// </summary>
        [Route(HttpVerbs.Get, "/api/element/type/{typeName}")]
        public async Task<IEnumerable<ElementModel>> GetByType(string typeName)
        {
            try
            {
                _logger.Information($"Getting elements by type: {typeName}");
                var elements = _revitApi.GetElementsByType(typeName);
                return elements;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error getting elements by type: {typeName}");
                throw new HttpException(500, ex.Message);
            }
        }

        /// <summary>
        /// Get element by ID
        /// </summary>
        [Route(HttpVerbs.Get, "/api/element/{elementId}")]
        public async Task<ElementModel> GetById(int elementId)
        {
            try
            {
                _logger.Information($"Getting element by ID: {elementId}");
                // Example implementation - would need to add a GetElementById method to RevitApiWrapper
                throw new NotImplementedException("GetElementById not implemented yet");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error getting element by ID: {elementId}");
                throw new HttpException(500, ex.Message);
            }
        }

        /// <summary>
        /// Modify element parameter
        /// </summary>
        [Route(HttpVerbs.Put, "/api/element/{elementId}/parameter")]
        public async Task<object> ModifyParameter(int elementId)
        {
            try
            {
                var json = await HttpContext.GetRequestBodyAsStringAsync();
                var model = JsonConvert.DeserializeObject<ParameterUpdateModel>(json);
                
                _logger.Information($"Modifying parameter {model.ParameterName} for element {elementId}");
                var success = _revitApi.ModifyElementParameter(elementId, model.ParameterName, model.Value);
                
                if (success)
                {
                    return new { success = true, message = "Parameter updated successfully" };
                }
                else
                {
                    throw new HttpException(400, "Failed to update parameter");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error modifying parameter for element {elementId}");
                throw new HttpException(500, ex.Message);
            }
        }

        /// <summary>
        /// MCP endpoint for element operations
        /// </summary>
        [Route(HttpVerbs.Post, "/api/element/mcp")]
        public async Task<McpResponse> ProcessMcpRequest()
        {
            try
            {
                var json = await HttpContext.GetRequestBodyAsStringAsync();
                var request = JsonConvert.DeserializeObject<ElementRequest>(json);
                
                _logger.Information($"Processing MCP request: {request.Action}");
                
                switch (request.Action)
                {
                    case "getElementsByCategory":
                        if (request.Parameters.TryGetValue("category", out var categoryObj) && categoryObj is string category)
                        {
                            var elements = _revitApi.GetElementsByCategory(category);
                            return McpResponse.Success(elements);
                        }
                        return McpResponse.Error("Missing or invalid category parameter");
                        
                    case "getElementsByType":
                        if (request.Parameters.TryGetValue("type", out var typeObj) && typeObj is string type)
                        {
                            var elements = _revitApi.GetElementsByType(type);
                            return McpResponse.Success(elements);
                        }
                        return McpResponse.Error("Missing or invalid type parameter");
                        
                    case "modifyElementParameter":
                        if (request.Parameters.TryGetValue("elementId", out var idObj) && 
                            request.Parameters.TryGetValue("parameterName", out var nameObj) &&
                            request.Parameters.TryGetValue("value", out var valueObj))
                        {
                            int elementId = Convert.ToInt32(idObj);
                            string paramName = nameObj.ToString();
                            string value = valueObj.ToString();
                            
                            var success = _revitApi.ModifyElementParameter(elementId, paramName, value);
                            return McpResponse.Success(new { success });
                        }
                        return McpResponse.Error("Missing or invalid parameters");
                        
                    default:
                        return McpResponse.Error($"Unknown action: {request.Action}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error processing MCP request");
                return McpResponse.Error(ex.Message);
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
