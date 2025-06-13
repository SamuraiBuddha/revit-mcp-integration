# Revit MCP Docker Toolkit Integration

This guide explains how to integrate the Revit MCP implementation with the Docker MCP Toolkit environment.

## Prerequisites

- Docker and Docker Compose installed
- Docker MCP Toolkit installed
- Autodesk Revit installed on the host machine
- Access to Revit API

## Integration Steps

### 1. Install Docker MCP Toolkit

If you haven't already, install the Docker MCP Toolkit:

```bash
docker plugin install docker/mcp-toolkit
```

### 2. Clone this Repository

```bash
git clone https://github.com/SamuraiBuddha/revit-mcp-integration.git
cd revit-mcp-integration
```

### 3. Configure MCP Settings

Edit the `mcp-config.json` file to configure your environment:

```json
{
  "registry": {
    "url": "http://localhost:8081"
  },
  "auth": {
    "token": "your-auth-token"
  },
  "tools": {
    "allowed": ["revit_element_query", "revit_dynamo_execute"]
  }
}
```

### 4. Build and Deploy

```bash
# Build the Docker images
docker compose -f docker-compose.mcp.yml build

# Start the MCP environment
docker compose -f docker-compose.mcp.yml up -d
```

### 5. Verify Installation

Check that the Revit MCP tool registered successfully:

```bash
docker exec mcp-registry mcp-cli list-tools
```

You should see `revit_element_query` and `revit_dynamo_execute` in the list.

## Using with LLMs

### OpenAI Integration

To use the Revit MCP tools with OpenAI:

1. Register the tools with OpenAI:

```python
from openai import OpenAI

client = OpenAI(api_key="your-api-key")

tools = [
  {
    "type": "function",
    "function": {
      "name": "revit_element_query",
      "description": "Query Revit elements by category, type, or properties",
      "parameters": {
        "type": "object",
        "properties": {
          "category": {
            "type": "string",
            "description": "Revit category (e.g., Walls, Doors, Windows)"
          },
          "filter": {
            "type": "string",
            "description": "Parameter filter expression"
          }
        },
        "required": ["category"]
      }
    }
  }
]

response = client.chat.completions.create(
  model="gpt-4",
  messages=[
    {"role": "system", "content": "You are a helpful BIM assistant."},
    {"role": "user", "content": "Find all fire-rated walls in my Revit model"}
  ],
  tools=tools
)
```

### Anthropic Claude Integration

For Anthropic Claude with MCP:

1. Set up the MCP endpoint in your application:

```python
import requests

def call_revit_mcp(tool_name, inputs):
    response = requests.post(
        "http://localhost:8080/v1/tools/invoke",
        json={
            "tool": tool_name,
            "inputs": inputs
        },
        headers={"Authorization": f"Bearer {MCP_AUTH_TOKEN}"}
    )
    return response.json()
```

2. Use with Claude:

```python
from anthropic import Anthropic

client = Anthropic(api_key="your-api-key")

# Get data from Revit
walls_data = call_revit_mcp("revit_element_query", {"category": "Walls"})

# Send to Claude
response = client.messages.create(
    model="claude-3-opus-20240229",
    max_tokens=1000,
    messages=[
        {"role": "user", "content": f"Analyze these walls from my Revit model: {walls_data}"}
    ]
)
```

## Troubleshooting

### Common Issues

1. **Connection Refused**: Ensure Revit is running and the add-in is loaded
2. **Authentication Errors**: Verify your MCP_AUTH_TOKEN is correct
3. **Tool Not Found**: Check that the tool is properly registered with the MCP registry

### Logs

Check Docker logs for issues:

```bash
docker logs revit-mcp-tool
docker logs mcp-registry
docker logs mcp-router
```

## Security Considerations

- The MCP integration exposes your Revit API to external tools
- Use authentication tokens and restrict access appropriately
- Consider network isolation for production environments

## License

This integration is provided under the MIT License.