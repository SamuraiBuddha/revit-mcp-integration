using System.Collections.Generic;
using Newtonsoft.Json;

namespace RevitMcpServer.Models
{
    /// <summary>
    /// Base class for MCP requests following the Model Context Protocol specification
    /// </summary>
    public class McpRequest
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Base class for MCP responses
    /// </summary>
    public class McpResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; } = "success";

        [JsonProperty("data")]
        public object Data { get; set; }

        [JsonProperty("error")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Create a successful response
        /// </summary>
        public static McpResponse Success(object data)
        {
            return new McpResponse
            {
                Status = "success",
                Data = data
            };
        }

        /// <summary>
        /// Create an error response
        /// </summary>
        public static McpResponse Error(string errorMessage)
        {
            return new McpResponse
            {
                Status = "error",
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Specialized MCP request for element operations
    /// </summary>
    public class ElementRequest : McpRequest
    {
        [JsonProperty("elementId")]
        public int ElementId { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("parameterName")]
        public string ParameterName { get; set; }

        [JsonProperty("parameterValue")]
        public string ParameterValue { get; set; }
    }

    /// <summary>
    /// Specialized MCP request for Dynamo script execution
    /// </summary>
    public class DynamoRequest : McpRequest
    {
        [JsonProperty("scriptPath")]
        public string ScriptPath { get; set; }

        [JsonProperty("parameters")]
        public new Dictionary<string, object> Parameters { get; set; }
    }
}
