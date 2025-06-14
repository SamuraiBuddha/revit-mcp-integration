using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using RevitMcpServer.Models;
using RevitMcpServer.UndergroundUtilities;

namespace RevitMcpServer.Controllers
{
    /// <summary>
    /// MCP endpoints for underground utilities operations
    /// </summary>
    public class UndergroundUtilitiesController : IMcpController
    {
        private readonly UIApplication _uiApp;
        private readonly IUndergroundUtilitiesEngine _utilitiesEngine;
        
        public UndergroundUtilitiesController(UIApplication uiApp)
        {
            _uiApp = uiApp;
            var doc = uiApp.ActiveUIDocument.Document;
            _utilitiesEngine = new UndergroundUtilitiesEngine(doc, null); // Point cloud analyzer injected as needed
        }

        /// <summary>
        /// Analyzes burial depths of utilities relative to finished grade
        /// </summary>
        [McpEndpoint("utilities/analyzeDepths")]
        public async Task<McpResponse> AnalyzeBurialDepths(McpRequest request)
        {
            try
            {
                var utilityIds = request.GetParameter<List<int>>("utilityIds");
                var surfaceId = request.GetParameter<int>("finishedGradeSurfaceId");
                
                var doc = _uiApp.ActiveUIDocument.Document;
                
                // Get utilities
                var utilities = utilityIds.Select(id => doc.GetElement(new ElementId(id)))
                    .Where(e => e != null)
                    .ToList();
                    
                // Get topography surface
                var surface = doc.GetElement(new ElementId(surfaceId)) as TopographySurface;
                if (surface == null)
                {
                    return McpResponse.Error("Finished grade surface not found");
                }
                
                // Analyze depths
                var analysis = _utilitiesEngine.AnalyzeBurialDepths(utilities, surface);
                
                return McpResponse.Success(new
                {
                    analyzedCount = analysis.PipeDepths.Count,
                    violations = analysis.PipeDepths
                        .Where(p => p.DepthViolations.Any())
                        .Select(p => new
                        {
                            elementId = p.ElementId.IntegerValue,
                            systemType = p.SystemType.ToString(),
                            minDepth = Math.Round(p.MinDepth, 2),
                            violations = p.DepthViolations.Select(v => v.Description)
                        }),
                    summary = new
                    {
                        averageDepth = Math.Round(analysis.PipeDepths.Average(p => p.AverageDepth), 2),
                        minDepth = Math.Round(analysis.PipeDepths.Min(p => p.MinDepth), 2),
                        maxDepth = Math.Round(analysis.PipeDepths.Max(p => p.MaxDepth), 2)
                    }
                });
            }
            catch (Exception ex)
            {
                return McpResponse.Error($"Depth analysis failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates utility corridors with clearance zones
        /// </summary>
        [McpEndpoint("utilities/generateCorridors")]
        public async Task<McpResponse> GenerateUtilityCorridors(McpRequest request)
        {
            try
            {
                var utilityIds = request.GetParameter<List<int>>("utilityIds");
                var groupingDistance = request.GetParameter<double>("groupingDistance", 10.0); // feet
                var includeExisting = request.GetParameter<bool>("includeExistingUtilities", true);
                
                var doc = _uiApp.ActiveUIDocument.Document;
                var utilities = GetUtilitiesByIds(utilityIds);
                
                var settings = new CorridorSettings
                {
                    GroupingDistance = groupingDistance,
                    IncludeExisting = includeExisting,
                    ClearanceStandards = LoadClearanceStandards()
                };
                
                // Generate corridors
                var corridors = _utilitiesEngine.GenerateUtilityCorridors(utilities, settings);
                
                return McpResponse.Success(new
                {
                    corridorCount = corridors.Count,
                    corridors = corridors.Select(c => new
                    {
                        utilityCount = c.Utilities.Count,
                        width = Math.Round(c.Width, 2),
                        length = Math.Round(c.Alignment.Length, 2),
                        clearanceZones = c.ClearanceZones.Count,
                        utilities = c.Utilities.Select(u => new
                        {
                            id = u.Id.IntegerValue,
                            type = GetUtilityType(u)
                        })
                    })
                });
            }
            catch (Exception ex)
            {
                return McpResponse.Error($"Corridor generation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts pipe inverts for gravity flow analysis
        /// </summary>
        [McpEndpoint("utilities/extractInverts")]
        public async Task<McpResponse> ExtractInvertElevations(McpRequest request)
        {
            try
            {
                var pipeIds = request.GetParameter<List<int>>("pipeIds");
                var includeSlope = request.GetParameter<bool>("includeSlope", true);
                
                var pipes = GetPipesByIds(pipeIds);
                var inverts = _utilitiesEngine.ExtractInvertElevations(pipes);
                
                return McpResponse.Success(new
                {
                    pipeCount = inverts.Count,
                    inverts = inverts.Select(i => new
                    {
                        pipeId = i.PipeId.IntegerValue,
                        upstreamInvert = Math.Round(i.UpstreamInvert, 3),
                        downstreamInvert = Math.Round(i.DownstreamInvert, 3),
                        slope = includeSlope ? Math.Round(i.Slope * 100, 2) : (double?)null, // As percentage
                        flowDirection = i.FlowDirection.ToString()
                    }),
                    statistics = new
                    {
                        averageSlope = Math.Round(inverts.Average(i => i.Slope) * 100, 2),
                        minSlope = Math.Round(inverts.Min(i => i.Slope) * 100, 2),
                        maxSlope = Math.Round(inverts.Max(i => i.Slope) * 100, 2)
                    }
                });
            }
            catch (Exception ex)
            {
                return McpResponse.Error($"Invert extraction failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs specialized underground clash detection
        /// </summary>
        [McpEndpoint("utilities/detectClashes")]
        public async Task<McpResponse> DetectUndergroundClashes(McpRequest request)
        {
            try
            {
                var existingIds = request.GetParameter<List<int>>("existingUtilityIds");
                var proposedIds = request.GetParameter<List<int>>("proposedUtilityIds");
                var checkClearances = request.GetParameter<bool>("checkClearances", true);
                
                var existingUtilities = GetUtilitiesByIds(existingIds);
                var proposedUtilities = GetUtilitiesByIds(proposedIds);
                
                var settings = new ClashDetectionSettings
                {
                    CheckClearances = checkClearances,
                    ClearanceMatrix = LoadClearanceMatrix(),
                    IncludeCrossings = true
                };
                
                // Detect clashes
                var clashes = _utilitiesEngine.DetectUndergroundClashes(
                    existingUtilities,
                    proposedUtilities,
                    settings
                );
                
                // Group by severity
                var clashGroups = clashes.GroupBy(c => c.Severity);
                
                return McpResponse.Success(new
                {
                    totalClashes = clashes.Count,
                    bySeverity = clashGroups.ToDictionary(
                        g => g.Key.ToString(),
                        g => g.Count()
                    ),
                    clashes = clashes.Select(c => new
                    {
                        existingId = c.ExistingUtility.Id.IntegerValue,
                        proposedId = c.ProposedUtility.Id.IntegerValue,
                        type = c.ClashType.ToString(),
                        severity = c.Severity.ToString(),
                        requiredClearance = Math.Round(c.RequiredClearance, 2),
                        actualClearance = Math.Round(c.ActualClearance, 2),
                        location = new
                        {
                            x = Math.Round(c.Location.X, 2),
                            y = Math.Round(c.Location.Y, 2),
                            z = Math.Round(c.Location.Z, 2)
                        }
                    })
                });
            }
            catch (Exception ex)
            {
                return McpResponse.Error($"Clash detection failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a complete underground utility network from scan data
        /// </summary>
        [McpEndpoint("utilities/createNetwork")]
        public async Task<McpResponse> CreateUndergroundNetwork(McpRequest request)
        {
            try
            {
                var detectedPipes = request.GetParameter<List<DetectedPipe>>("detectedPipes");
                var material = request.GetParameter<string>("defaultMaterial", "PVC");
                var levelName = request.GetParameter<string>("level", "Underground");
                
                var doc = _uiApp.ActiveUIDocument.Document;
                var level = GetOrCreateLevel(levelName, -10.0); // Default 10ft below grade
                
                var settings = new NetworkCreationSettings
                {
                    LevelId = level.Id,
                    DefaultMaterial = material,
                    AutoGenerateStructures = true,
                    StructureSpacing = 300.0, // 300ft max between structures
                    MaterialMappings = GetMaterialMappings()
                };
                
                // Create network
                var network = await _utilitiesEngine.CreateUndergroundNetwork(detectedPipes, settings);
                
                return McpResponse.Success(new
                {
                    createdPipes = network.Pipes.Count,
                    createdStructures = network.Structures.Count,
                    errors = network.Errors.Count,
                    network = new
                    {
                        pipeIds = network.Pipes.Select(p => p.Id.IntegerValue),
                        structureIds = network.Structures.Select(s => s.Id.IntegerValue),
                        totalLength = Math.Round(network.Pipes.Sum(p => GetPipeLength(p)), 2)
                    },
                    errorDetails = network.Errors
                });
            }
            catch (Exception ex)
            {
                return McpResponse.Error($"Network creation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Integrates GPR data with point cloud for comprehensive utility mapping
        /// </summary>
        [McpEndpoint("utilities/integrateGPR")]
        public async Task<McpResponse> IntegrateGPRData(McpRequest request)
        {
            try
            {
                var gprDataPath = request.GetParameter<string>("gprDataPath");
                var pointCloudId = request.GetParameter<string>("pointCloudId");
                var coordinateSystem = request.GetParameter<string>("coordinateSystem");
                
                // Load GPR data
                var gprData = LoadGPRDataset(gprDataPath);
                var pointCloud = GetPointCloudInstance(pointCloudId);
                var transform = GetCoordinateTransform(coordinateSystem);
                
                // Integrate data
                var merged = await _utilitiesEngine.IntegrateGPRData(gprData, pointCloud, transform);
                
                return McpResponse.Success(new
                {
                    verifiedUtilities = merged.VerifiedUtilities.Count,
                    gprOnlyUtilities = merged.GPROnlyUtilities.Count,
                    totalDetected = merged.VerifiedUtilities.Count + merged.GPROnlyUtilities.Count,
                    summary = new
                    {
                        verified = merged.VerifiedUtilities.Select(u => new
                        {
                            type = u.Type.ToString(),
                            depth = Math.Round(u.Depth, 2),
                            confidence = Math.Round(u.Confidence, 2)
                        }),
                        gprOnly = merged.GPROnlyUtilities.Select(u => new
                        {
                            type = u.UtilityType.ToString(),
                            depth = Math.Round(u.Depth, 2),
                            confidence = Math.Round(u.Confidence, 2)
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                return McpResponse.Error($"GPR integration failed: {ex.Message}");
            }
        }

        #region Helper Methods

        private List<Element> GetUtilitiesByIds(List<int> ids)
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            return ids.Select(id => doc.GetElement(new ElementId(id)))
                .Where(e => e != null)
                .ToList();
        }

        private List<Pipe> GetPipesByIds(List<int> ids)
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            return ids.Select(id => doc.GetElement(new ElementId(id)))
                .OfType<Pipe>()
                .ToList();
        }

        private string GetUtilityType(Element element)
        {
            if (element is Pipe pipe)
            {
                var system = pipe.MEPSystem;
                if (system != null)
                {
                    return system.SystemType.ToString();
                }
                return "Pipe";
            }
            else if (element is Conduit)
            {
                return "Electrical Conduit";
            }
            else if (element is CableTray)
            {
                return "Cable Tray";
            }
            else if (element is Duct)
            {
                return "HVAC Duct";
            }
            
            return element.Category?.Name ?? "Unknown";
        }

        private Level GetOrCreateLevel(string levelName, double elevation)
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            
            // Try to find existing level
            var existingLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == levelName);
                
            if (existingLevel != null)
            {
                return existingLevel;
            }
            
            // Create new level
            using (var trans = new Transaction(doc, "Create Level"))
            {
                trans.Start();
                var newLevel = Level.Create(doc, elevation);
                newLevel.Name = levelName;
                trans.Commit();
                return newLevel;
            }
        }

        private double GetPipeLength(Pipe pipe)
        {
            var curve = (pipe.Location as LocationCurve)?.Curve;
            return curve?.Length ?? 0.0;
        }

        private Dictionary<string, string> GetMaterialMappings()
        {
            return new Dictionary<string, string>
            {
                { "Storm Sewer", "RCP" }, // Reinforced Concrete Pipe
                { "Sanitary Sewer", "PVC" },
                { "Water Main", "DI" }, // Ductile Iron
                { "Gas Main", "PE" }, // Polyethylene
                { "Electrical", "PVC" },
                { "Telecom", "HDPE" }
            };
        }

        private ClearanceMatrix LoadClearanceMatrix()
        {
            // Load from configuration or standards database
            // This is a simplified version
            return new ClearanceMatrix();
        }

        private List<ClearanceStandard> LoadClearanceStandards()
        {
            // Load from configuration or standards database
            return new List<ClearanceStandard>();
        }

        #endregion
    }
}
