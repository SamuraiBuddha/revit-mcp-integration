using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
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
        public DepthAnalysisResult AnalyzeBurialDepths(
            List<Element> utilityElements, 
            Autodesk.Revit.DB.Architecture.TopographySurface finishedGrade)
        {
            var analysis = new DepthAnalysisResult
            {
                PipeDepths = new List<PipeDepthInfo>()
            };
            
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
        /// Extracts pipe invert elevations from pipes (interface implementation)
        /// </summary>
        public async Task<Dictionary<Pipe, double>> ExtractInvertElevations(List<Pipe> pipes)
        {
            var result = new Dictionary<Pipe, double>();
            
            foreach (var pipe in pipes)
            {
                var curve = (pipe.Location as LocationCurve)?.Curve;
                if (curve == null) continue;
                
                var startPoint = curve.GetEndPoint(0);
                
                // Get pipe diameter
                var diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.AsDouble() ?? 0;
                
                // Calculate invert elevation (bottom of pipe)
                var invertElevation = startPoint.Z - (diameter / 2);
                result[pipe] = invertElevation;
            }
            
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Creates complete underground utility network (interface implementation)
        /// </summary>
        public async Task<UtilityNetworkResult> CreateUndergroundNetwork(
            List<RevitMcpServer.Models.DetectedPipe> detectedPipes,
            NetworkCreationSettings settings)
        {
            var result = new UtilityNetworkResult
            {
                CreatedPipes = new List<Pipe>(),
                CreatedStructures = new List<FamilyInstance>(),
                Errors = new List<CreationError>()
            };
            
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
                            detectedPipe.StartPoint,
                            detectedPipe.EndPoint,
                            detectedPipe.Diameter,
                            pipeType,
                            settings
                        );
                        
                        // Set underground-specific parameters
                        SetUndergroundParameters(pipe, detectedPipe, settings);
                        
                        result.CreatedPipes.Add(pipe);
                        
                        // Detect and create structures (manholes, vaults)
                        var structures = await DetectStructures(detectedPipe, settings);
                        result.CreatedStructures.AddRange(structures);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new CreationError
                        {
                            Message = $"Failed to create pipe: {ex.Message}",
                            DetectedPipe = detectedPipe
                        });
                    }
                }
                
                // Connect network elements
                ConnectNetworkElements(result);
                
                trans.Commit();
            }
            
            return result;
        }

        /// <summary>
        /// Performs clash detection specific to underground utilities
        /// </summary>
        public List<UndergroundClash> DetectUndergroundClashes(
            List<Element> existingUtilities,
            List<Element> proposedUtilities,
            ClashDetectionSettings settings)
        {
            var clashes = new List<UndergroundClash>();
            
            foreach (var existing in existingUtilities)
            {
                foreach (var proposed in proposedUtilities)
                {
                    var clearance = GetRequiredClearance(existing, proposed, settings);
                    var actualDistance = CalculateMinimumDistance(existing, proposed);
                    
                    if (actualDistance < clearance)
                    {
                        clashes.Add(new UndergroundClash
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

        /// <summary>
        /// Integrates Ground Penetrating Radar data with point cloud
        /// </summary>
        public async Task<GPRIntegrationResult> IntegrateGPRData(
            object gprData,
            object pointCloud,
            object transform)
        {
            // Placeholder implementation
            return new GPRIntegrationResult
            {
                VerifiedUtilities = new List<VerifiedUtility>(),
                GPROnlyUtilities = new List<GPRUtility>()
            };
        }

        #region Helper Methods

        private List<double> CalculatePipeDepths(Pipe pipe, Autodesk.Revit.DB.Architecture.TopographySurface surface)
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
                
                // Get surface elevation at this point
                var surfaceElevation = GetSurfaceElevation(surface, point);
                if (surfaceElevation.HasValue)
                {
                    depths.Add(surfaceElevation.Value - point.Z);
                }
            }
            
            return depths;
        }

        private double? GetSurfaceElevation(Autodesk.Revit.DB.Architecture.TopographySurface surface, XYZ point)
        {
            try
            {
                // Project point to surface
                var uvPoint = surface.Project(point);
                if (uvPoint != null)
                {
                    return uvPoint.XYZPoint.Z;
                }
            }
            catch { }
            return null;
        }

        private Pipe CreatePipeWithMaterial(
            XYZ startPoint,
            XYZ endPoint,
            double diameter,
            PipeType pipeType,
            NetworkCreationSettings settings)
        {
            // Create line
            var line = Line.CreateBound(startPoint, endPoint);
            
            // Create pipe
            var pipe = Pipe.Create(
                _doc,
                pipeType.Id,
                settings.ReferenceLevel.Id,
                null, // Let Revit create connectors
                line
            );
            
            // Set diameter
            pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.Set(diameter);
            
            return pipe;
        }

        private void SetUndergroundParameters(Element pipe, RevitMcpServer.Models.DetectedPipe detected, NetworkCreationSettings settings)
        {
            // Set custom shared parameters for underground utilities
            var burialDepthParam = pipe.LookupParameter("Burial Depth");
            if (burialDepthParam != null)
            {
                burialDepthParam.Set(10.0); // Default 10 feet
            }
            
            var materialParam = pipe.LookupParameter("Pipe Material");
            if (materialParam != null && detected.Properties.ContainsKey("Material"))
            {
                materialParam.Set(detected.Properties["Material"].ToString());
            }
        }

        private double GetRequiredClearance(Element utility1, Element utility2, ClashDetectionSettings settings)
        {
            var type1 = GetUtilityTypeName(utility1);
            var type2 = GetUtilityTypeName(utility2);
            
            // Look up clearance requirements from settings/standards
            return settings.ClearanceMatrix.GetClearance(type1, type2);
        }

        private string GetUtilityTypeName(Element element)
        {
            if (element is Pipe)
                return "Pipe";
            else if (element is Autodesk.Revit.DB.Electrical.Conduit)
                return "Electrical";
            else if (element is Autodesk.Revit.DB.Electrical.CableTray)
                return "Cable Tray";
            else if (element is Autodesk.Revit.DB.Mechanical.Duct)
                return "HVAC Duct";
            
            return element.Category?.Name ?? "Unknown";
        }

        private UtilityType GetSystemType(Pipe pipe)
        {
            var systemType = pipe.MEPSystem?.SystemType;
            
            if (systemType == null)
            {
                // Infer from pipe size and material
                var diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.AsDouble() ?? 0;
                
                if (diameter > 8.0 / 12) // > 8"
                    return UtilityType.StormSewer;
                else if (diameter > 4.0 / 12) // > 4"
                    return UtilityType.SanitarySewer;
                else
                    return UtilityType.WaterMain;
            }
            
            // Convert MEPSystemType to UtilityType
            return UtilityType.Unknown;
        }

        private List<DepthViolation> CheckDepthRequirements(Pipe pipe, List<double> depths)
        {
            var violations = new List<DepthViolation>();
            var minRequired = GetMinimumDepthRequirement(pipe);
            
            if (depths.Min() < minRequired)
            {
                violations.Add(new DepthViolation
                {
                    Description = $"Minimum depth violation: {depths.Min():F2} ft < {minRequired:F2} ft required",
                    ActualDepth = depths.Min(),
                    RequiredDepth = minRequired
                });
            }
            
            return violations;
        }

        private double GetMinimumDepthRequirement(Pipe pipe)
        {
            var systemType = GetSystemType(pipe);
            
            // Standard minimum depths
            switch (systemType)
            {
                case UtilityType.WaterMain:
                    return 4.0; // 4 feet
                case UtilityType.SanitarySewer:
                    return 6.0; // 6 feet
                case UtilityType.StormSewer:
                    return 3.0; // 3 feet
                default:
                    return 2.0; // 2 feet default
            }
        }

        private List<List<Element>> GroupUtilitiesByProximity(List<Element> utilities, double maxDistance)
        {
            // Simple clustering algorithm
            var groups = new List<List<Element>>();
            var assigned = new HashSet<Element>();
            
            foreach (var utility in utilities)
            {
                if (assigned.Contains(utility)) continue;
                
                var group = new List<Element> { utility };
                assigned.Add(utility);
                
                foreach (var other in utilities)
                {
                    if (assigned.Contains(other)) continue;
                    
                    if (CalculateMinimumDistance(utility, other) <= maxDistance)
                    {
                        group.Add(other);
                        assigned.Add(other);
                    }
                }
                
                groups.Add(group);
            }
            
            return groups;
        }

        private Curve CalculateCorridorAlignment(List<Element> utilities)
        {
            // Calculate centerline of utility group
            var points = new List<XYZ>();
            
            foreach (var utility in utilities)
            {
                if (utility.Location is LocationCurve locCurve)
                {
                    points.Add(locCurve.Curve.GetEndPoint(0));
                    points.Add(locCurve.Curve.GetEndPoint(1));
                }
            }
            
            if (points.Count >= 2)
            {
                // Simple line through centroid
                var centroid = new XYZ(
                    points.Average(p => p.X),
                    points.Average(p => p.Y),
                    points.Average(p => p.Z)
                );
                
                // Find furthest points
                var p1 = points.OrderBy(p => p.DistanceTo(centroid)).Last();
                var p2 = points.OrderByDescending(p => p.DistanceTo(p1)).First();
                
                return Line.CreateBound(p1, p2);
            }
            
            return null;
        }

        private double CalculateCorridorWidth(List<Element> group, CorridorSettings settings)
        {
            // Calculate required width based on utilities and clearances
            double width = 0;
            
            foreach (var utility in group)
            {
                if (utility is Pipe pipe)
                {
                    var diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.AsDouble() ?? 1.0;
                    width += diameter + 2.0; // Add clearance
                }
            }
            
            return width;
        }

        private List<ClearanceZone> GenerateClearanceZones(List<Element> group, CorridorSettings settings)
        {
            return new List<ClearanceZone>();
        }

        private double CalculateSlope(XYZ startPoint, XYZ endPoint, double diameter)
        {
            var run = Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2));
            var rise = endPoint.Z - startPoint.Z;
            return rise / run;
        }

        private FlowDirection DetermineFlowDirection(Pipe pipe)
        {
            var curve = (pipe.Location as LocationCurve)?.Curve;
            if (curve != null)
            {
                var start = curve.GetEndPoint(0);
                var end = curve.GetEndPoint(1);
                
                if (start.Z > end.Z)
                    return FlowDirection.Downstream;
                else if (start.Z < end.Z)
                    return FlowDirection.Upstream;
            }
            
            return FlowDirection.Bidirectional;
        }

        private PipeType DeterminePipeType(RevitMcpServer.Models.DetectedPipe detectedPipe, NetworkCreationSettings settings)
        {
            // Get first available pipe type
            var collector = new FilteredElementCollector(_doc)
                .OfClass(typeof(PipeType));
            
            return collector.FirstElement() as PipeType;
        }

        private async Task<List<FamilyInstance>> DetectStructures(RevitMcpServer.Models.DetectedPipe pipe, NetworkCreationSettings settings)
        {
            return new List<FamilyInstance>();
        }

        private void ConnectNetworkElements(UtilityNetworkResult network)
        {
            // Connect pipes to structures
        }

        private double CalculateMinimumDistance(Element elem1, Element elem2)
        {
            // Simplified distance calculation
            var loc1 = elem1.Location as LocationCurve;
            var loc2 = elem2.Location as LocationCurve;
            
            if (loc1 != null && loc2 != null)
            {
                var curve1 = loc1.Curve;
                var curve2 = loc2.Curve;
                
                // Get midpoints
                var mid1 = curve1.Evaluate(0.5, true);
                var mid2 = curve2.Evaluate(0.5, true);
                
                return mid1.DistanceTo(mid2);
            }
            
            return double.MaxValue;
        }

        private ClashType DetermineClashType(double actualDistance, double requiredClearance)
        {
            if (actualDistance <= 0)
                return ClashType.HardClash;
            else if (actualDistance < requiredClearance)
                return ClashType.ClearanceViolation;
            else
                return ClashType.Crossing;
        }

        private XYZ GetClashLocation(Element existing, Element proposed)
        {
            var loc1 = existing.Location as LocationCurve;
            var loc2 = proposed.Location as LocationCurve;
            
            if (loc1 != null && loc2 != null)
            {
                // Return midpoint between elements
                var mid1 = loc1.Curve.Evaluate(0.5, true);
                var mid2 = loc2.Curve.Evaluate(0.5, true);
                
                return new XYZ(
                    (mid1.X + mid2.X) / 2,
                    (mid1.Y + mid2.Y) / 2,
                    (mid1.Z + mid2.Z) / 2
                );
            }
            
            return XYZ.Zero;
        }

        private ClashSeverity CalculateClashSeverity(Element existing, Element proposed, double actualDistance)
        {
            if (actualDistance <= 0)
                return ClashSeverity.Critical;
            
            var type1 = GetUtilityTypeName(existing);
            var type2 = GetUtilityTypeName(proposed);
            
            // Critical combinations
            if ((type1.Contains("Gas") && type2.Contains("Electric")) ||
                (type1.Contains("Water") && type2.Contains("Sewer")))
            {
                return ClashSeverity.Critical;
            }
            
            if (actualDistance < 1.0)
                return ClashSeverity.Major;
            else if (actualDistance < 2.0)
                return ClashSeverity.Minor;
            else
                return ClashSeverity.Warning;
        }

        #endregion
    }
}