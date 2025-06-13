using System;

namespace RevitMcpServer.Models
{
    /// <summary>
    /// Attribute to mark methods as MCP endpoints
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class McpEndpointAttribute : Attribute
    {
        public string Path { get; }

        public McpEndpointAttribute(string path)
        {
            Path = path;
        }
    }
}
