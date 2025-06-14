# Revit MCP Integration

A Model Context Protocol (MCP) server for Autodesk Revit, enabling AI systems to interact with Building Information Models (BIM) through standardized protocols.

## Overview

This project provides a bridge between Autodesk Revit and AI systems using the Model Context Protocol (MCP). It allows language models and other AI tools to query and manipulate Revit models through a simple HTTP API.

## Core Concept

The integration works as a Revit add-in that:
1. Loads when Revit starts
2. Runs an HTTP server using the MCP protocol
3. Exposes Revit API functionality through MCP-compatible endpoints
4. Allows external tools (AI assistants, n8n workflows, etc.) to interact with Revit

## Project Goals

### Phase 1: Basic Proof of Concept (Current)
- Establish basic MCP server within Revit
- Implement simple test endpoints (get Revit version, list elements)
- Ensure stable communication between external clients and Revit

### Phase 2: Core Functionality
- Basic element creation and modification
- Element querying and filtering
- Property reading and writing

### Phase 3: Advanced Features (Future)
- Scan-to-BIM processing
- Underground utilities specialization
- Real-time collaboration
- ML-powered automation

## Technical Stack

- **Revit Add-in**: .NET Framework/Core compatible with Revit 2024
- **HTTP Server**: EmbedIO for lightweight HTTP handling
- **Protocol**: Model Context Protocol (MCP) for standardized AI interaction
- **Target**: Autodesk Revit 2024+

## Current Status

See [PROJECT_STATUS.md](PROJECT_STATUS.md) for detailed task tracking and progress.

## Development Philosophy

1. **Incremental Development**: Build and test one feature at a time
2. **Stability First**: Ensure each component works before adding complexity
3. **Clear Documentation**: Maintain concise, actionable documentation
4. **Token-Aware**: Keep documentation focused to work within AI context limits

## Getting Started

### Prerequisites
- Autodesk Revit 2024
- Visual Studio 2022 or later
- .NET SDK (latest compatible version)

### Basic Setup
1. Clone the repository
2. Open `revit-mcp-integration.sln` in Visual Studio
3. Build the solution
4. Copy the add-in files to Revit's addins folder
5. Start Revit and verify the add-in loads

## Repository Structure

```
revit-mcp-integration/
├── RevitMcpServer/          # Core Revit add-in
│   ├── Controllers/         # MCP endpoint handlers
│   ├── Models/             # Data models
│   └── McpServer.cs        # Main server implementation
├── PROJECT_STATUS.md       # Current tasks and progress
└── README.md              # This file
```

## License

MIT License - see LICENSE file for details.
