# Revit MCP Integration

A comprehensive integration between Revit, Model Context Protocol (MCP), and n8n for advanced BIM automation and AI-assisted workflows.

## Overview

This project enables AI systems to interact directly with Autodesk Revit through the Model Context Protocol (MCP), providing a standardized way for language models and other AI tools to query and manipulate Building Information Models (BIM).

## Components

The integration consists of three main components:

1. **Revit MCP Server**: A .NET-based Revit add-in that exposes Revit API functionality through MCP-compatible endpoints
2. **n8n Custom Nodes**: Custom nodes for the n8n workflow automation platform that communicate with the Revit MCP Server
3. **RAG Integration**: Integration with vector and graph databases for knowledge-enhanced interactions

## System Requirements

- Autodesk Revit 2023 or newer
- .NET Framework 4.8 or .NET 6.0+
- n8n workflow automation platform
- Windows 10/11 (for Revit integration)
- Docker environment (for n8n and database components)

## Project Structure

```
revit-mcp/
├── RevitMcpServer/                     # .NET Revit Add-in
│   ├── RevitMcpServer.csproj           # Project file
│   ├── RevitMcpServer.addin            # Revit add-in manifest
│   ├── McpServer.cs                    # MCP server implementation
│   ├── RevitApiWrapper.cs              # Wrapper for Revit API calls
│   ├── Controllers/                    # API controllers
│   │   ├── ElementController.cs        # Element operations
│   │   └── DynamoController.cs         # Dynamo script execution
│   └── Models/                         # Data models
│       ├── ElementModel.cs             # Element representation
│       └── McpRequest.cs               # MCP request/response models
├── n8n-nodes-revit-mcp/                # n8n custom nodes
│   ├── package.json                    # Node package definition
│   ├── nodes/                          # Custom node implementations
│   │   ├── RevitMcp/                   # Revit MCP node
│   │   │   ├── RevitMcp.node.ts        # Node implementation
│   │   │   └── RevitMcp.node.json      # Node metadata
│   │   └── RevitRag/                   # Revit RAG node
│   │       ├── RevitRag.node.ts        # Node implementation
│   │       └── RevitRag.node.json      # Node metadata
│   └── credentials/                    # Credential types
│       └── RevitMcpApi.credentials.ts  # API credentials
└── docker/                             # Docker configuration
    ├── docker-compose.yml              # Compose file for n8n + databases
    └── n8n-workflows/                  # Example n8n workflows
        ├── revit-element-query.json    # Element query workflow
        ├── revit-dynamo-execution.json # Dynamo execution workflow
        └── revit-rag-assistant.json    # RAG-enhanced assistant workflow
```

## Getting Started

1. Build and install the Revit MCP Server add-in
2. Install the n8n custom nodes
3. Set up the Docker environment
4. Import example workflows

Detailed instructions for each step are provided in the respective component directories.

## Features

- Query Revit model elements by category, parameter values, or spatial relationships
- Execute Dynamo scripts from n8n workflows
- Modify element parameters programmatically
- Integrate with LLMs for natural language BIM interaction
- Connect model elements to building codes and standards through RAG
- Generate documentation and reports based on model data
- Create visualization outputs for model analysis

## Development Status

This project is currently in proof-of-concept stage. Key components are functional but not production-ready.

## License

This project is licensed under the MIT License - see the LICENSE file for details.