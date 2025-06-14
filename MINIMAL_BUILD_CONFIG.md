# Minimal Build Configuration

## Purpose
This document defines what components should be included in the minimal proof-of-concept build to avoid compilation errors and establish a stable core.

## Components to Include

### Core Files
- McpServer.cs - Main server implementation (already simplified)
- RevitApiWrapper.cs - Basic Revit API wrapper
- RevitMcpServer.addin - Addin manifest
- RevitMcpServer.csproj - Project file

### Controllers (Active)
- ElementController.cs - Basic element operations only

### Controllers (To Disable)
- DynamoController.cs - Complex, requires Dynamo references
- ScanToBIMController.cs - Complex, requires ML/point cloud processing
- ScanToBIMControllerHelpers.cs - Supporting file for ScanToBIM
- UndergroundUtilitiesController.cs - Complex, specialized functionality

### Directories to Exclude from Build
- /ScanToBIM/* - All scan-to-BIM processing files
- /UndergroundUtilities/* - All underground utilities files
- /Services/* - ML and other services not needed for POC

## Build Steps for Minimal POC

1. Comment out or remove complex controller registrations in McpServer.cs
2. Exclude complex directories from project compilation
3. Focus on basic element CRUD operations
4. Test with simple endpoints:
   - GET /api/health
   - GET /api/revit/version
   - GET /api/elements (list elements)
   - POST /api/elements/wall (create simple wall)

## Next Steps After POC Works

Once the minimal build is stable:
1. Add features one at a time
2. Test thoroughly before adding next feature
3. Document each feature's dependencies
4. Create modular architecture for optional features
