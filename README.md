# Revit MCP Integration

A comprehensive integration between Revit, Model Context Protocol (MCP), and n8n for advanced BIM automation and AI-assisted workflows, with specialized capabilities for scan-to-BIM and underground utilities.

## Overview

This project enables AI systems to interact directly with Autodesk Revit through the Model Context Protocol (MCP), providing a standardized way for language models and other AI tools to query and manipulate Building Information Models (BIM). It includes advanced features for:

- **Scan-to-BIM Processing**: AI-powered point cloud analysis for automatic element detection
- **Underground Utilities**: Specialized tools for subsurface infrastructure modeling
- **Real-time Collaboration**: WebSocket-based communication for live model updates
- **ML Integration**: GPU-accelerated machine learning for object detection and classification

## Key Features

### Scan-to-BIM Capabilities
- **Point Cloud Analysis**
  - ML-based detection of pipes, ducts, conduits, and cable trays
  - Automatic classification of MEP systems
  - Structural element extraction (walls, columns, beams, slabs)
  - Confidence scoring and validation

- **Intelligent Element Creation**
  - Automatic pipe routing from centerlines
  - Smart fitting placement at intersections
  - Material identification from scan data
  - Batch processing for large datasets

### Underground Utilities Specialization
- **Depth Analysis**
  - Burial depth calculation relative to finished grade
  - Depth violation detection per utility type
  - Slope analysis for gravity systems

- **Utility Corridors**
  - 3D corridor generation with clearance zones
  - Multi-utility coordination
  - Conflict detection with existing infrastructure

- **Advanced Features**
  - GPR (Ground Penetrating Radar) data integration
  - Invert elevation extraction
  - Network connectivity validation
  - Automated structure placement (manholes, vaults)

## Components

### 1. **Revit MCP Server**
A .NET-based Revit add-in that exposes Revit API functionality through MCP-compatible endpoints

### 2. **Specialized Modules**
- `ScanToBIM/`: Point cloud processing and element detection
- `UndergroundUtilities/`: Subsurface infrastructure tools
- `Controllers/`: MCP endpoint implementations

### 3. **n8n Integration**
Custom nodes for workflow automation with specialized operations:
- Scan data processing workflows
- Underground utility network creation
- Automated clash detection and resolution

### 4. **Infrastructure Stack**
- **PostgreSQL + pgvector**: Spatial data and vector embeddings
- **Neo4j**: BIM relationship graphs
- **Qdrant**: Semantic search for scan data
- **MinIO**: Object storage for point clouds
- **InfluxDB**: Time-series data for monitoring
- **Grafana**: Visualization dashboards
- **ML Server**: GPU-accelerated inference

## System Requirements

- Autodesk Revit 2023 or newer
- .NET Framework 4.8 or .NET 6.0+
- Windows 10/11 (for Revit integration)
- Docker environment
- NVIDIA GPU (recommended for ML features)
- 32GB+ RAM for large point cloud processing

## Project Structure

```
revit-mcp-integration/
├── RevitMcpServer/                      # .NET Revit Add-in
│   ├── Controllers/                     # MCP endpoints
│   │   ├── ElementController.cs         # Basic operations
│   │   ├── ScanToBIMController.cs      # Scan processing
│   │   └── UndergroundUtilitiesController.cs
│   ├── ScanToBIM/                      # Point cloud modules
│   │   ├── PointCloudAnalyzer.cs      # ML detection
│   │   └── ScanToRevitConverter.cs    # Element creation
│   ├── UndergroundUtilities/           # Utility tools
│   │   └── UndergroundUtilitiesEngine.cs
│   └── Models/                         # Data models
├── n8n-nodes-revit-mcp/                # n8n custom nodes
│   ├── nodes/                          # Node implementations
│   └── credentials/                    # Auth types
├── docker/                             # Docker configuration
│   ├── docker-compose.yml              # Full stack definition
│   ├── n8n-workflows/                  # Example workflows
│   │   └── scan-to-bim-underground-utilities.json
│   ├── ml-server/                      # ML model serving
│   └── revit-ws-bridge/               # WebSocket bridge
└── models/                             # Trained ML models
```

## Getting Started

### 1. Install Revit Add-in
```bash
# Build the .NET solution
cd RevitMcpServer
dotnet build --configuration Release

# Copy to Revit addins folder
copy bin\Release\RevitMcpServer.* "%APPDATA%\Autodesk\Revit\Addins\2024\"
```

### 2. Start Docker Stack
```bash
cd docker
docker compose up -d

# For GPU support
docker compose --profile gpu-nvidia up -d
```

### 3. Configure n8n
- Access n8n at http://localhost:5678
- Import example workflows from `docker/n8n-workflows/`
- Configure MCP credentials to connect to Revit

### 4. Initialize ML Models
```bash
# Download pre-trained models
./scripts/download-models.sh

# Or train custom models
docker exec -it jupyter jupyter lab
# Navigate to /notebooks/train-pointcloud-models.ipynb
```

## Usage Examples

### Scan-to-BIM Processing
```javascript
// In n8n workflow or via API
const scanResult = await mcpClient.call('scan/detectPipes', {
  regionId: 'scan-region-1',
  confidenceThreshold: 0.85
});

// Automatically create Revit elements
const created = await mcpClient.call('scan/createPipes', {
  detectedPipes: scanResult.pipes,
  autoConnect: true
});
```

### Underground Utilities
```javascript
// Analyze burial depths
const depthAnalysis = await mcpClient.call('utilities/analyzeDepths', {
  utilityIds: [12345, 12346, 12347],
  finishedGradeSurfaceId: 98765
});

// Detect clashes with clearance checking
const clashes = await mcpClient.call('utilities/detectClashes', {
  existingUtilityIds: existing,
  proposedUtilityIds: proposed,
  checkClearances: true
});
```

## API Endpoints

### Scan-to-BIM Operations
- `POST /scan/detectPipes` - Detect pipes in point cloud
- `POST /scan/classifyMEP` - Classify MEP systems
- `POST /scan/createPipes` - Create pipes from detection
- `POST /scan/placeFittings` - Auto-place fittings

### Underground Utilities
- `POST /utilities/analyzeDepths` - Analyze burial depths
- `POST /utilities/generateCorridors` - Create utility corridors
- `POST /utilities/extractInverts` - Get pipe inverts
- `POST /utilities/detectClashes` - Find conflicts
- `POST /utilities/createNetwork` - Build complete network
- `POST /utilities/integrateGPR` - Merge GPR data

## Configuration

### Environment Variables
```bash
# PostgreSQL
POSTGRES_PASSWORD=your_password

# Neo4j
NEO4J_PASSWORD=your_password

# MinIO (for point clouds)
MINIO_ROOT_USER=admin
MINIO_ROOT_PASSWORD=your_password

# ML Server
CUDA_VISIBLE_DEVICES=0
MODEL_PATH=/models
```

### MCP Settings
Configure in `RevitMcpServer/appsettings.json`:
```json
{
  "MCP": {
    "Port": 7891,
    "MaxConnections": 10,
    "EnableWebSocket": true,
    "Authentication": {
      "Type": "Bearer",
      "Secret": "your-secret-key"
    }
  },
  "ScanToBIM": {
    "ConfidenceThreshold": 0.85,
    "MLModelPath": "\\models\\pointcloud",
    "BatchSize": 1000
  }
}
```

## Development

### Adding New MCP Endpoints
1. Create controller in `RevitMcpServer/Controllers/`
2. Implement endpoint logic using Revit API
3. Register in `McpServer.cs`
4. Add corresponding n8n node in `n8n-nodes-revit-mcp/`

### Training Custom ML Models
The project includes Jupyter notebooks for training models on your specific data:
- `train-pipe-detection.ipynb` - Train pipe detection model
- `train-mep-classification.ipynb` - Train MEP system classifier
- `train-material-identification.ipynb` - Train material classifier

### Extending Underground Utilities
To add new utility types or standards:
1. Update `ClearanceMatrix` in `UndergroundUtilitiesEngine.cs`
2. Add material mappings in `GetMaterialMappings()`
3. Implement custom depth requirements in `CheckDepthRequirements()`

## Performance Optimization

### Point Cloud Processing
- Use GPU acceleration when available
- Process in batches of 1000-5000 points
- Implement spatial indexing for large datasets
- Cache processed regions

### Network Operations
- Use WebSocket for real-time updates
- Batch API calls when possible
- Implement connection pooling
- Use Redis for caching

## Troubleshooting

### Common Issues

**Revit Add-in Not Loading**
- Check .addin file path
- Verify .NET version compatibility
- Review Revit journal file for errors

**Point Cloud Detection Failures**
- Verify ML model is loaded
- Check point cloud density
- Adjust confidence threshold
- Ensure GPU drivers are updated

**Docker Services Not Starting**
```bash
# Check logs
docker compose logs [service-name]

# Verify network
docker network ls

# Check resource usage
docker stats
```

### Debug Mode
Enable detailed logging in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "RevitMCP": "Trace"
    }
  }
}
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Standards
- Follow C# coding conventions for .NET code
- Use TypeScript for n8n nodes
- Include unit tests for new features
- Document API endpoints with OpenAPI

## Roadmap

### Phase 1 (Current)
- ✅ Basic MCP integration
- ✅ Scan-to-BIM pipeline
- ✅ Underground utilities tools
- ✅ n8n workflow automation

### Phase 2 (Q3 2024)
- [ ] Unreal Engine integration for visualization
- [ ] Advanced ML models for complex geometries
- [ ] Real-time collaboration features
- [ ] Mobile app for field data collection

### Phase 3 (Q4 2024)
- [ ] Cloud deployment options
- [ ] Multi-project coordination
- [ ] Integration with BMS systems
- [ ] Predictive maintenance features

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Anthropic for developing the Model Context Protocol
- Autodesk for the Revit API
- n8n community for workflow automation
- Open source ML libraries (PyTorch, TensorFlow)

## Support

For questions, issues, or feature requests:
- Open an issue on GitHub
- Join our Discord community
- Email: support@revit-mcp.com

---

Built with ❤️ for the AEC industry
