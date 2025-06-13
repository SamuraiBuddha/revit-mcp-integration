using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMcpServer.Models;
using RevitMcpServer.ScanToBIM;
using RevitMcpServer.UndergroundUtilities;

namespace RevitMcpServer.Controllers
{
    /// <summary>
    /// MCP endpoints for scan-to-BIM operations
    /// </summary>
    public class ScanToBIMController : IMcpController
    {
        private readonly UIApplication _uiApp;
        private readonly IPointCloudAnalyzer _pointCloudAnalyzer;
        private readonly IScanToRevitConverter _scanToRevitConverter;
        
        public ScanToBIMController(UIApplication uiApp)
        {
            _uiApp = uiApp;
            var doc = uiApp.ActiveUIDocument.Document;
            
            // Initialize services
            _pointCloudAnalyzer = new PointCloudAnalyzer(doc, new MLService());
            _scanToRevitConverter = new ScanToRevitConverter(doc);
        }

        /// <summary>
        /// Detects pipes from point cloud data
        /// </summary>
        [McpEndpoint("scan/detectPipes")]
        public async Task<McpResponse> DetectPipes(McpRequest request)
        {
            try
            {
                var regionId = request.GetParameter<string>("regionId");
                var confidenceThreshold = request.GetParameter<double>("confidenceThreshold", 0.85);
                
                // Get point cloud region
                var pointCloud = GetPointCloudRegion(regionId);
                if (pointCloud == null)
                {
                    return McpResponse.Error("Point cloud region not found");
                }
                
                // Detect pipes
                var detectedPipes = await _pointCloudAnalyzer.DetectPipes(pointCloud, confidenceThreshold);
                
                return McpResponse.Success(new
                {
                    pipeCount = detectedPipes.Count,
                    pipes = detectedPipes.Select(p => new
                    {
                        id = Guid.NewGuid().ToString(),
                        diameter = p.Diameter * 12, // Convert to inches
                        length = p.Centerline.Length,
                        confidence = p.Confidence,
                        material = p.Material.ToString(),
                        systemType = p.SystemType.ToString()
                    })
                });
            }
            catch (Exception ex)
            {
                return McpResponse.Error($"Pipe detection failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Classifies MEP systems from scan data
        /// </summary>
        [McpEndpoint("scan/classifyMEP")]
        public async Task<McpResponse> ClassifyMEPSystems(McpRequest request)
        {
            try
            {
                var scanDataId = request.GetParameter<string>("scanDataId");
                
                // Get detected objects from scan
                var objects = GetDetectedObjects(scanDataId);
                
                // Classify MEP systems
                var classification = await _pointCloudAnalyzer.ClassifyMEPSystems(objects);
                
                return McpResponse.Success(new
                {
                    summary = new
                    {
                        hvacDucts = classification.HVACDucts.Count,
                        pipes = classification.Pipes.Count,
                        conduits = classification.Conduits.Count,
                        cableTrays = classification.CableTrays.Count
                    },
                    classification = classification
                });
            }
            catch (Exception ex)
            {
                return McpResponse.Error($"MEP classification failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates Revit elements from detected pipes
        /// </summary>
        [McpEndpoint("scan/createPipes")]
        public async Task<McpResponse> CreatePipesFromScan(McpRequest request)
        {
            try
            {
                var detectedPipes = request.GetParameter<List<DetectedPipe>>("detectedPipes");
                var levelName = request.GetParameter<string>("level", "Level 1");
                var autoConnect = request.GetParameter<bool>("autoConnect", true);
                
                var settings = new PipeCreationSettings
                {
                    ReferenceLevel = GetLevelByName(levelName),
                    AutoSizeEnabled = true
                };
                
                // Create pipes in Revit
                var createdPipes = await _scanToRevitConverter.CreatePipesFromCenterlines(
                    detectedPipes, 
                    settings
                );
                
                return McpResponse.Success(new
                {
                    createdCount = createdPipes.Count,
                    elementIds = createdPipes.Select(p => p.Id.IntegerValue),
                    message = $"Successfully created {createdPipes.Count} pipes from scan data"
                });
            }
            catch (Exception ex)
            {
                return McpResponse.Error($"Pipe creation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyzes point cloud for underground utilities
        /// </summary>
        [McpEndpoint("scan/detectUndergroundUtilities")]
        public async Task<McpResponse> DetectUndergroundUtilities(McpRequest request)
        {
            try
            {
                var regionId = request.GetParameter<string>("regionId");
                var groundSurfaceId = request.GetParameter<int>("groundSurfaceId");
                
                var region = GetPointCloudRegion(regionId);
                var groundSurface = GetGroundSurface(groundSurfaceId);
                
                // Detect underground pipes with depth analysis
                var undergroundPipes = await _pointCloudAnalyzer.DetectUndergroundPipes(
                    region,
                    groundSurface
                );
                
                return McpResponse.Success(new
                {
                    pipeCount = undergroundPipes.Count,
                    utilities = undergroundPipes.Select(u => new
                    {
                        diameter = u.Pipe.Diameter * 12,
                        material = u.Material.ToString(),
                        averageDepth = u.BurialDepths.Average(),
                        minDepth = u.BurialDepths.Min(),
                        maxDepth = u.BurialDepths.Max(),
                        condition = u.Condition.ToString()
                    })
                });
            }
            catch (Exception ex)
            {
                return McpResponse.Error($"Underground utility detection failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs intelligent fitting placement at intersections
        /// </summary>
        [McpEndpoint("scan/placeFittings")]
        public async Task<McpResponse> PlaceFittingsAtIntersections(McpRequest request)
        {
            try
            {
                var pipeIds = request.GetParameter<List<int>>("pipeIds");
                var autoSelect = request.GetParameter<bool>("autoSelectFittings", true);
                
                var pipes = GetPipesByIds(pipeIds);
                var intersections = FindPipeIntersections(pipes);
                var placedFittings = new List<FamilyInstance>();
                
                foreach (var intersection in intersections)
                {
                    try
                    {
                        var fitting = await _scanToRevitConverter.GenerateFittingAtIntersection(
                            intersection.Pipe1,
                            intersection.Pipe2,
                            intersection
                        );
                        
                        if (fitting != null)
                        {
                            placedFittings.Add(fitting);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but continue with other fittings
                        LogWarning($"Failed to place fitting: {ex.Message}");
                    }
                }
                
                return McpResponse.Success(new
                {
                    placedCount = placedFittings.Count,
                    fittingIds = placedFittings.Select(f => f.Id.IntegerValue),
                    message = $"Placed {placedFittings.Count} fittings at intersections"
                });
            }
            catch (Exception ex)
            {
                return McpResponse.Error($"Fitting placement failed: {ex.Message}");
            }
        }

        #region Helper Methods

        private PointCloudRegion GetPointCloudRegion(string regionId)
        {
            // Implementation to get point cloud region by ID
            var doc = _uiApp.ActiveUIDocument.Document;
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(PointCloudInstance));
                
            // Find point cloud and extract region
            // This is a simplified version - actual implementation would be more complex
            return null;
        }

        private Level GetLevelByName(string levelName)
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == levelName);
                
            if (collector == null)
            {
                throw new InvalidOperationException($"Level '{levelName}' not found");
            }
            
            return collector;
        }

        private List<IntersectionAnalysis> FindPipeIntersections(List<Pipe> pipes)
        {
            var intersections = new List<IntersectionAnalysis>();
            
            for (int i = 0; i < pipes.Count - 1; i++)
            {
                for (int j = i + 1; j < pipes.Count; j++)
                {
                    var intersection = AnalyzePipeIntersection(pipes[i], pipes[j]);
                    if (intersection != null)
                    {
                        intersections.Add(intersection);
                    }
                }
            }
            
            return intersections;
        }

        #endregion
    }
}
