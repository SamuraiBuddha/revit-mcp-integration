# Revit MCP Integration - Troubleshooting Guide

## Endpoint Connection Issues

If the endpoints are not responding after deployment, check the following:

### 1. Check Revit Logs
Look for logs at: `C:\Users\[YourUsername]\AppData\Local\RevitMcpServer\logs`

### 2. Verify Addon Loaded in Revit
- Open Revit 2024
- Go to Add-Ins tab
- Check if RevitMcpServer appears
- Look for any error messages

### 3. Common Issues and Solutions

#### Issue: Server Not Starting
**Symptoms**: No response from endpoints, no logs created
**Possible Causes**:
- Revit addon not loading
- Exception during startup
- Port already in use

**Solutions**:
1. Check Windows Event Viewer for errors
2. Try a different port (modify McpServer.cs)
3. Add more logging to debug startup

#### Issue: Threading Problems
**Symptoms**: Server starts but doesn't respond
**Cause**: Revit API threading restrictions

**Solution**: May need to adjust how the web server is started

#### Issue: Firewall Blocking
**Symptoms**: Can't connect to localhost:7891
**Solution**: 
1. Check Windows Firewall settings
2. Try running `netsh http add urlacl url=http://localhost:7891/ user=Everyone`

### 4. Debug Steps
1. Add console output to verify addon loads
2. Add try-catch blocks with TaskDialog messages
3. Verify server actually starts
4. Check if ApplicationInitialized event fires

### 5. Alternative Testing
Instead of curl, try:
- Opening http://localhost:7891/api/health in a browser
- Using PowerShell: `Invoke-WebRequest http://localhost:7891/api/health`
- Using Postman or similar tool

### 6. Quick Debug Version
We can create a debug version with TaskDialog messages to see where it fails.
