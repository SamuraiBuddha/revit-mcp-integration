# Testing Guide - Revit MCP Server

## Quick Start

1. **Build and Deploy**
   ```
   build-and-deploy.bat
   ```
   This will:
   - Build the project
   - Copy files to Revit addins folder
   - Show next steps

2. **Start Revit 2024**
   - Open Revit 2024
   - The add-in should load automatically
   - No UI elements - it runs in the background

3. **Verify Add-in Loaded**
   Check the log file:
   ```
   %LOCALAPPDATA%\RevitMcpServer\logs\revit-mcp-[date].log
   ```
   Look for:
   - "RevitMcpServer starting up"
   - "Revit Version: 2024"
   - "Starting MCP server on http://localhost:7891/"

4. **Test Endpoints**
   Run the PowerShell test script:
   ```
   powershell -ExecutionPolicy Bypass -File test-endpoints.ps1
   ```
   
   Or test manually with curl:
   ```
   curl http://localhost:7891/api/health
   curl http://localhost:7891/api/revit/version
   ```

## Expected Results

### Health Endpoint
```json
{
  "status": "ok",
  "timestamp": "2025-06-14T04:30:00Z",
  "service": "RevitMcpServer"
}
```

### Version Endpoint
```json
{
  "versionNumber": "2024",
  "versionName": "Autodesk Revit 2024",
  "versionBuild": "20230308_1515(x64)",
  "subVersionNumber": "2024.0.2",
  "language": "English_USA"
}
```

## Troubleshooting

### Add-in doesn't load
1. Check if .addin file is in correct location:
   `%APPDATA%\Autodesk\Revit\Addins\2024\RevitMcpServer.addin`
2. Verify all DLLs are present in same folder
3. Check Revit journal file for errors:
   `%LOCALAPPDATA%\Autodesk\Revit\Autodesk Revit 2024\Journals\`

### Server not responding
1. Check if Revit is running
2. Check logs for startup errors
3. Verify port 7891 is not in use:
   ```
   netstat -an | findstr 7891
   ```

### Build errors
1. Ensure .NET SDK is installed
2. Verify Revit 2024 is installed at default location
3. Check if Revit API DLLs exist:
   `C:\Program Files\Autodesk\Revit 2024\RevitAPI.dll`

## Next Steps

Once basic endpoints are working:
1. Implement MCP protocol wrapper
2. Add element listing endpoint
3. Add element creation endpoint
4. Build MCP client for testing

## Current Limitations

- No authentication
- No WebSocket support
- Synchronous operations only
- Basic error handling
- No transaction management yet

---

Last Updated: June 14, 2025
