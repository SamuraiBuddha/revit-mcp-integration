using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RevitMcpServer.Models
{
    /// <summary>
    /// Base class for MCP requests following the Model Context Protocol specification
    /// </summary>
    public class McpRequest
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Base class for MCP responses
    /// </summary>
    public class McpResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "success";

        [JsonPropertyName("data")]
        public object Data { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }

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
                Error = errorMessage
            };
        }
    }

    /// <summary>
    /// Specialized MCP request for element operations
    /// </summary>
    public class ElementRequest : McpRequest
    {
        [JsonPropertyName("elementId")]
        public int ElementId { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("parameterName")]
        public string ParameterName { get; set; }

        [JsonPropertyName("parameterValue")]
        public string ParameterValue { get; set; }
    }

    /// <summary>
    /// Specialized MCP request for Dynamo script execution
    /// </summary>
    public class DynamoRequest : McpRequest
    {
        [JsonPropertyName("scriptPath")]
        public string ScriptPath { get; set; }

        [JsonPropertyName("parameters")]
        public new Dictionary<string, object> Parameters { get; set; }
    }
}