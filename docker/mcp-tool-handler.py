#!/usr/bin/env python3
"""
MCP Tool Handler for Revit MCP Server

This script bridges between the MCP Toolkit protocol and the Revit MCP Server.
It handles:
1. Registering tools with the MCP registry
2. Processing incoming MCP tool requests
3. Translating between MCP protocol and Revit MCP Server API
4. Managing authentication and error handling
"""

import os
import sys
import json
import time
import logging
import subprocess
import threading
import requests
from datetime import datetime
from typing import Dict, Any, List, Optional

# Configure logging
logging.basicConfig(
    level=logging.DEBUG if os.environ.get("MCP_TOOL_LOG_LEVEL", "").lower() == "debug" else logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
)
logger = logging.getLogger("revit-mcp-tool")

# Configuration from environment variables
REGISTRY_URL = os.environ.get("MCP_REGISTRY_URL", "http://localhost:8081")
TOOL_ID = os.environ.get("MCP_TOOL_ID", "revit")
AUTH_TOKEN = os.environ.get("MCP_TOOL_AUTH_TOKEN", "default-dev-token")
TOOL_PORT = int(os.environ.get("MCP_TOOL_PORT", "8082"))
REVIT_HOST = os.environ.get("REVIT_HOST", "localhost")
REVIT_PORT = int(os.environ.get("REVIT_PORT", "5000"))

# Path to tool schema definitions
SCHEMA_DIR = "/app/schemas"

class RevitMcpToolHandler:
    """Handler for Revit MCP Tool requests"""
    
    def __init__(self):
        self.tool_schemas = self._load_tool_schemas()
        self.revit_server_process = None
        
    def _load_tool_schemas(self) -> Dict[str, Dict[str, Any]]:
        """Load tool schemas from JSON files"""
        schemas = {}
        for filename in os.listdir(SCHEMA_DIR):
            if filename.endswith(".json"):
                tool_name = filename.split(".")[0]
                with open(os.path.join(SCHEMA_DIR, filename), "r") as f:
                    schemas[tool_name] = json.load(f)
        return schemas
    
    def start_revit_server(self):
        """Start the Revit MCP Server as a subprocess"""
        logger.info("Starting Revit MCP Server...")
        self.revit_server_process = subprocess.Popen(
            ["dotnet", "RevitMcpServer.dll"],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
        
        # Start a thread to monitor the process output
        threading.Thread(
            target=self._monitor_process_output,
            args=(self.revit_server_process,),
            daemon=True,
        ).start()
        
        # Wait for server to start
        self._wait_for_server_ready()
        
    def _monitor_process_output(self, process):
        """Monitor and log the process output"""
        for line in iter(process.stdout.readline, b''):
            logger.info(f"RevitServer: {line.decode().strip()}")
        for line in iter(process.stderr.readline, b''):
            logger.error(f"RevitServer: {line.decode().strip()}")
    
    def _wait_for_server_ready(self, max_retries=30, retry_interval=1):
        """Wait for the Revit MCP Server to be ready"""
        logger.info(f"Waiting for Revit MCP Server at http://{REVIT_HOST}:{REVIT_PORT}...")
        
        for _ in range(max_retries):
            try:
                response = requests.get(f"http://{REVIT_HOST}:{REVIT_PORT}/api/status")
                if response.status_code == 200:
                    logger.info("Revit MCP Server is ready")
                    return
            except requests.exceptions.ConnectionError:
                pass
            
            time.sleep(retry_interval)
        
        logger.error(f"Revit MCP Server not ready after {max_retries * retry_interval} seconds")
        raise RuntimeError("Failed to connect to Revit MCP Server")
    
    def register_tools(self):
        """Register tools with the MCP registry"""
        logger.info(f"Registering tools with MCP registry at {REGISTRY_URL}...")
        
        for tool_name, schema in self.tool_schemas.items():
            tool_id = f"{TOOL_ID}.{tool_name}"
            
            registration_data = {
                "id": tool_id,
                "name": schema.get("name", tool_name),
                "description": schema.get("description", ""),
                "version": schema.get("version", "1.0.0"),
                "input_schema": schema.get("input_schema", {}),
                "output_schema": schema.get("output_schema", {}),
                "endpoint": f"http://revit-mcp-tool:{TOOL_PORT}/tools/{tool_name}",
            }
            
            try:
                response = requests.post(
                    f"{REGISTRY_URL}/v1/tools",
                    json=registration_data,
                    headers={"Authorization": f"Bearer {AUTH_TOKEN}"},
                )
                
                if response.status_code in (200, 201):
                    logger.info(f"Successfully registered tool: {tool_id}")
                else:
                    logger.error(f"Failed to register tool {tool_id}: {response.status_code} - {response.text}")
            except Exception as e:
                logger.error(f"Error registering tool {tool_id}: {str(e)}")
    
    def translate_mcp_to_revit(self, tool_name: str, mcp_request: Dict[str, Any]) -> Dict[str, Any]:
        """Translate MCP request to Revit MCP Server format"""
        if tool_name == "revit_element_query":
            # Map MCP inputs to Revit MCP Server request
            category = mcp_request.get("inputs", {}).get("category", "")
            filter_expr = mcp_request.get("inputs", {}).get("filter", "")
            
            return {
                "action": "getElementsByCategory",
                "parameters": {
                    "category": category,
                    "filterExpression": filter_expr,
                }
            }
        
        elif tool_name == "revit_dynamo_execute":
            # Map MCP inputs to Revit MCP Server request
            script_path = mcp_request.get("inputs", {}).get("script_path", "")
            parameters = mcp_request.get("inputs", {}).get("parameters", {})
            
            return {
                "action": "runDynamoScript",
                "parameters": {
                    "scriptPath": script_path,
                    "scriptParameters": parameters,
                }
            }
        
        else:
            raise ValueError(f"Unknown tool: {tool_name}")
    
    def translate_revit_to_mcp(self, tool_name: str, revit_response: Dict[str, Any]) -> Dict[str, Any]:
        """Translate Revit MCP Server response to MCP format"""
        if revit_response.get("status") == "error":
            return {
                "status": "error",
                "error": revit_response.get("error", "Unknown error"),
            }
        
        # Extract the actual data from the Revit response
        data = revit_response.get("data", {})
        
        if tool_name == "revit_element_query":
            return {
                "status": "success",
                "result": data,
            }
        
        elif tool_name == "revit_dynamo_execute":
            return {
                "status": "success",
                "result": {
                    "success": True,
                    "results": data,
                }
            }
        
        else:
            raise ValueError(f"Unknown tool: {tool_name}")
    
    def handle_tool_request(self, tool_name: str, request_data: Dict[str, Any]) -> Dict[str, Any]:
        """Handle an incoming MCP tool request"""
        logger.info(f"Handling request for tool: {tool_name}")
        
        try:
            # Translate MCP request to Revit format
            revit_request = self.translate_mcp_to_revit(tool_name, request_data)
            
            # Determine endpoint based on tool
            if tool_name == "revit_element_query":
                endpoint = f"http://{REVIT_HOST}:{REVIT_PORT}/api/element/mcp"
            elif tool_name == "revit_dynamo_execute":
                endpoint = f"http://{REVIT_HOST}:{REVIT_PORT}/api/dynamo/mcp"
            else:
                raise ValueError(f"Unknown tool: {tool_name}")
            
            # Send request to Revit MCP Server
            response = requests.post(
                endpoint,
                json=revit_request,
                headers={"Content-Type": "application/json"},
            )
            
            if response.status_code != 200:
                return {
                    "status": "error",
                    "error": f"Revit MCP Server returned status {response.status_code}: {response.text}",
                }
            
            # Translate Revit response to MCP format
            revit_response = response.json()
            return self.translate_revit_to_mcp(tool_name, revit_response)
        
        except Exception as e:
            logger.exception(f"Error handling tool request: {str(e)}")
            return {
                "status": "error",
                "error": str(e),
            }
    
    def shutdown(self):
        """Shutdown the handler and cleanup resources"""
        if self.revit_server_process:
            logger.info("Stopping Revit MCP Server...")
            self.revit_server_process.terminate()
            self.revit_server_process.wait(timeout=5)
        logger.info("Revit MCP Tool Handler shutdown complete")

def main():
    """Main entry point"""
    logger.info("Starting Revit MCP Tool Handler...")
    
    handler = RevitMcpToolHandler()
    
    try:
        # Start the Revit MCP Server
        handler.start_revit_server()
        
        # Register tools with MCP registry
        handler.register_tools()
        
        # Start HTTP server to handle tool requests
        from http.server import HTTPServer, BaseHTTPRequestHandler
        
        class MCP_ToolRequestHandler(BaseHTTPRequestHandler):
            def do_POST(self):
                content_length = int(self.headers['Content-Length'])
                post_data = self.rfile.read(content_length)
                request_data = json.loads(post_data.decode('utf-8'))
                
                # Extract tool name from path
                tool_name = self.path.strip('/').split('/')[-1]
                
                # Handle the request
                response_data = handler.handle_tool_request(tool_name, request_data)
                
                # Send response
                self.send_response(200)
                self.send_header('Content-type', 'application/json')
                self.end_headers()
                self.wfile.write(json.dumps(response_data).encode('utf-8'))
            
            def log_message(self, format, *args):
                logger.info(f"HTTP: {self.address_string()} - {format % args}")
        
        # Start the HTTP server
        server = HTTPServer(('0.0.0.0', TOOL_PORT), MCP_ToolRequestHandler)
        logger.info(f"MCP Tool HTTP server started on port {TOOL_PORT}")
        
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            pass
        finally:
            server.server_close()
    
    except Exception as e:
        logger.exception(f"Fatal error: {str(e)}")
        sys.exit(1)
    finally:
        handler.shutdown()

if __name__ == "__main__":
    main()
