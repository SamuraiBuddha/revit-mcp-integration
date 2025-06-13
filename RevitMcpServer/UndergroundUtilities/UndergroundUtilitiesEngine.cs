using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using RevitMcpServer.Models;
using RevitMcpServer.ScanToBIM;

namespace RevitMcpServer.UndergroundUtilities
{
    /// <summary>
    /// Specialized tools for underground utility modeling and coordination
    /// </summary>
    public class UndergroundUtilitiesEngine : IUndergroundUtilitiesEngine
    {
        private readonly Document _doc;
        private readonly IPointCloudAnalyzer _pointCloudAnalyzer;
        
        public UndergroundUtilitiesEngine(Document doc, IPointCloudAnalyzer pointCloudAnalyzer)
        {
            _doc = doc;
            _pointCloudAnalyzer = pointCloudAnalyzer;
        }

        /// <summary>
        /// Analyzes burial depths relative to finished grade
        /// </summary>
        public UtilityDepthAnalysis AnalyzeBurialDepths(
            List<Element> utilityElements, 
            TopographySurface finishedGrade)
        {
            var analysis = new UtilityDepthAnalysis();
            
            foreach (var element in utilityElements)
            {
                if (element is Pipe pipe)
                {
                    var depths = CalculatePipeDepths(pipe, finishedGrade);
                    analysis.PipeDepths.Add(new PipeDepthInfo
                    {
                        ElementId = pipe.Id,
                        SystemType = GetSystemType(pipe),
                        MinDepth = depths.Min(),
                        MaxDepth = depths.Max(),
                        AverageDepth = depths.Average(),
                        DepthViolations = CheckDepthRequirements(pipe, depths)
                    });
                }
            }
            
            return analysis;
        }

        /// <summary>
        /// Creates 3D utility corridors with clearance zones
        /// </summary>
        public List<UtilityCorridor> GenerateUtilityCorridors(
            List<Element> utilities,
            CorridorSettings settings)
        {
            var corridors = new List<UtilityCorridor>();
            
            // Group utilities by proximity
            var utilityGroups = GroupUtilitiesByProximity(utilities, settings.GroupingDistance);
            
            foreach (var group in utilityGroups)
            {
                var corridor = new UtilityCorridor
                {
                    Utilities = group,
                    Alignment = CalculateCorridorAlignment(group),
                    Width = CalculateCorridorWidth(group, settings),
                    ClearanceZones = GenerateClearanceZones(group, settings)
                };
                
                corridors.Add(corridor);
            }
            
            return corridors;
        }

        /// <summary>
        /// Integrates Ground Penetrating Radar data with point cloud
        /// </summary>
        public async Task<MergedUtilityData> IntegrateGPRData(
            GPRDataset gprData,
            PointCloudInstance pointCloud,
            CoordinateTransform transform)
        {
            var merged = new MergedUtilityData();
            
            // Convert GPR data to Revit coordinates
            var gprObjects = ConvertGPRToRevitCoordinates(gprData, transform);
            
            // Match GPR detections with point cloud objects
            foreach (var gprObject in gprObjects)
            {
                var pointCloudMatch = FindPointCloudMatch(gprObject, pointCloud);
                
                if (pointCloudMatch != null)
                {
                    // Merge data - use point cloud for accurate position, GPR for depth
                    merged.VerifiedUtilities.Add(new VerifiedUtility
                    {
                        Location = pointCloudMatch.Location,
                        Depth = gprObject.Depth,
                        Type = gprObject.UtilityType,
                        Confidence = Math.Min(gprObject.Confidence, pointCloudMatch.Confidence)
                    });
                }
                else
                {
                    // GPR-only detection (buried utility not visible in scan)
                    merged.GPROnlyUtilities.Add(gprObject);
                }
            }
            
            return merged;
        }

        /// <summary>
        /// Automatically extracts pipe inverts critical for gravity systems
        /// </summary>
        public List<InvertElevation> ExtractInvertElevations(List<Pipe> pipes)
        {
            var inverts = new List<InvertElevation>();
            
            foreach (var pipe in pipes)
            {
                var curve = (pipe.Location as LocationCurve)?.Curve;
                if (curve == null) continue;
                
                var startPoint = curve.GetEndPoint(0);
                var endPoint = curve.GetEndPoint(1);
                
                // Get pipe diameter
                var diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.AsDouble() ?? 0;
                
                inverts.Add(new InvertElevation
                {
                    PipeId = pipe.Id,
                    UpstreamInvert = startPoint.Z - (diameter / 2),
                    DownstreamInvert = endPoint.Z - (diameter / 2),
                    Slope = CalculateSlope(startPoint, endPoint, diameter),
                    FlowDirection = DetermineFlowDirection(pipe)
                });
            }
            
            return inverts;
        }

        /// <summary>
        /// Creates complete underground utility networks
        /// </summary>
        public async Task<UtilityNetwork> CreateUndergroundNetwork(
            List<DetectedPipe> detectedPipes,
            NetworkCreationSettings settings)
        {
            var network = new UtilityNetwork();
            
            using (var trans = new Transaction(_doc, "Create Underground Utilities"))
            {
                trans.Start();
                
                foreach (var detectedPipe in detectedPipes)
                {
                    try
                    {
                        // Determine pipe type based on size and context
                        var pipeType = DeterminePipeType(detectedPipe, settings);
                        
                        // Create pipe with appropriate material
                        var pipe = CreatePipeWithMaterial(
                            detectedPipe.Centerline,
                            detectedPipe.Diameter,
                            pipeType,
                            settings
                        );
                        
                        // Set underground-specific parameters
                        SetUndergroundParameters(pipe, detectedPipe, settings);
                        
                        network.Pipes.Add(pipe);
                        
                        // Detect and create structures (manholes, vaults)
                        var structures = await DetectStructures(detectedPipe, settings);
                        network.Structures.AddRange(structures);
                    }
                    catch (Exception ex)
                    {
                        network.Errors.Add($"Failed to create pipe: {ex.Message}");
                    }
                }
                
                // Connect network elements
                ConnectNetworkElements(network);
                
                trans.Commit();
            }
            
            return network;
        }

        /// <summary>
        /// Performs clash detection specific to underground utilities
        /// </summary>
        public List<UtilityClash> DetectUndergroundClashes(
            List<Element> existingUtilities,
            List<Element> proposedUtilities,
            ClashDetectionSettings settings)
        {
            var clashes = new List<UtilityClash>();
            
            foreach (var existing in existingUtilities)
            {
                foreach (var proposed in proposedUtilities)
                {
                    var clearance = GetRequiredClearance(existing, proposed, settings);
                    var actualDistance = CalculateMinimumDistance(existing, proposed);
                    
                    if (actualDistance < clearance)
                    {
                        clashes.Add(new UtilityClash
                        {
                            ExistingUtility = existing,
                            ProposedUtility = proposed,
                            ClashType = DetermineClashType(actualDistance, clearance),
                            RequiredClearance = clearance,
                            ActualClearance = actualDistance,
                            Location = GetClashLocation(existing, proposed),
                            Severity = CalculateClashSeverity(existing, proposed, actualDistance)
                        });
                    }
                }
            }
            
            return clashes;
        }

        #region Helper Methods

        private List<double> CalculatePipeDepths(Pipe pipe, TopographySurface surface)
        {
            var depths = new List<double>();
            var curve = (pipe.Location as LocationCurve)?.Curve;
            if (curve == null) return depths;
            
            // Sample points along pipe
            const int samples = 20;
            for (int i = 0; i <= samples; i++)
            {
                var param = i / (double)samples;
                var point = curve.Evaluate(param, true);
                
                // Project to surface
                var surfacePoint = surface.FindPoints(point.X, point.Y).FirstOrDefault();
                if (surfacePoint != null)
                {
                    depths.Add(surfacePoint.Z - point.Z);
                }
            }
            
            return depths;
        }

        private Pipe CreatePipeWithMaterial(
            Line centerline,
            double diameter,
            PipeType pipeType,
            NetworkCreationSettings settings)
        {
            // Get appropriate pipe material based on settings
            var material = settings.MaterialMappings[pipeType.Name] ?? "PVC";
            
            // Create pipe
            var pipe = Pipe.Create(
                _doc,
                pipeType.Id,
                settings.LevelId,
                null, // Let Revit create connectors
                centerline
            );
            
            // Set diameter
            pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.Set(diameter);
            
            // Set material parameters
            SetPipeMaterial(pipe, material);
            
            return pipe;
        }

        private void SetUndergroundParameters(Pipe pipe, DetectedPipe detected, NetworkCreationSettings settings)
        {
            // Set custom shared parameters for underground utilities
            var burialDepthParam = pipe.LookupParameter("Burial Depth");
            if (burialDepthParam != null && detected.BurialDepths?.Any() == true)
            {
                burialDepthParam.Set(detected.BurialDepths.Average());
            }
            
            var materialParam = pipe.LookupParameter("Pipe Material");
            if (materialParam != null)
            {
                materialParam.Set(detected.Material.ToString());
            }
            
            var conditionParam = pipe.LookupParameter("Condition Assessment");
            if (conditionParam != null && detected.Condition != null)
            {
                conditionParam.Set(detected.Condition.ToString());
            }
            
            // Set pipe class/schedule based on depth and type
            SetPipeClass(pipe, detected, settings);
        }

        private double GetRequiredClearance(Element utility1, Element utility2, ClashDetectionSettings settings)
        {
            var type1 = GetUtilityType(utility1);
            var type2 = GetUtilityType(utility2);
            
            // Look up clearance requirements from settings/standards
            return settings.ClearanceMatrix.GetClearance(type1, type2);
        }

        private MEPSystemType GetSystemType(Pipe pipe)
        {
            var systemType = pipe.MEPSystem?.SystemType;
            
            if (systemType == null)
            {
                // Infer from pipe size and material
                var diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.AsDouble() ?? 0;
                
                if (diameter > 8.0 / 12) // > 8"
                    return MEPSystemType.StormWater;
                else if (diameter > 4.0 / 12) // > 4"
                    return MEPSystemType.Sanitary;
                else
                    return MEPSystemType.DomesticWater;
            }
            
            return ConvertToMEPSystemType(systemType);
        }

        #endregion
    }

    #region Supporting Classes

    public class UtilityDepthAnalysis
    {
        public List<PipeDepthInfo> PipeDepths { get; set; } = new List<PipeDepthInfo>();
        public List<DepthViolation> Violations { get; set; } = new List<DepthViolation>();
    }

    public class PipeDepthInfo
    {
        public ElementId ElementId { get; set; }
        public MEPSystemType SystemType { get; set; }
        public double MinDepth { get; set; }
        public double MaxDepth { get; set; }
        public double AverageDepth { get; set; }
        public List<DepthViolation> DepthViolations { get; set; }
    }

    public class UtilityCorridor
    {
        public List<Element> Utilities { get; set; }
        public Curve Alignment { get; set; }
        public double Width { get; set; }
        public List<ClearanceZone> ClearanceZones { get; set; }
    }

    public class UtilityNetwork
    {
        public List<Pipe> Pipes { get; set; } = new List<Pipe>();
        public List<FamilyInstance> Structures { get; set; } = new List<FamilyInstance>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class UtilityClash
    {
        public Element ExistingUtility { get; set; }
        public Element ProposedUtility { get; set; }
        public ClashType ClashType { get; set; }
        public double RequiredClearance { get; set; }
        public double ActualClearance { get; set; }
        public XYZ Location { get; set; }
        public ClashSeverity Severity { get; set; }
    }

    public enum ClashType
    {
        HardClash,
        ClearanceViolation,
        CrossingConflict,
        ParallelConflict
    }

    public enum ClashSeverity
    {
        Critical,
        Major,
        Minor,
        Warning
    }

    #endregion
}
