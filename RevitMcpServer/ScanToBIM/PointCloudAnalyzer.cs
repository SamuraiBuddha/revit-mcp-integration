using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using RevitMcpServer.Models;

namespace RevitMcpServer.ScanToBIM
{
    /// <summary>
    /// AI-powered point cloud analysis for detecting MEP systems and structural elements
    /// </summary>
    public class PointCloudAnalyzer : IPointCloudAnalyzer
    {
        private readonly Document _doc;
        private readonly IMLService _mlService;
        
        public PointCloudAnalyzer(Document doc, IMLService mlService)
        {
            _doc = doc;
            _mlService = mlService;
        }

        /// <summary>
        /// Detects pipes in point cloud region using ML cylindrical detection
        /// </summary>
        public async Task<List<DetectedPipe>> DetectPipes(PointCloudRegion region, double confidenceThreshold = 0.85)
        {
            var detectedPipes = new List<DetectedPipe>();
            
            // Extract point cloud data for ML processing
            var points = ExtractPointsFromRegion(region);
            
            // Use YOLO-based cylindrical detection
            var cylinders = await _mlService.DetectCylinders(points);
            
            foreach (var cylinder in cylinders.Where(c => c.Confidence >= confidenceThreshold))
            {
                // Validate as pipe based on diameter and orientation
                if (IsPipeLikeCylinder(cylinder))
                {
                    var pipe = new DetectedPipe
                    {
                        Centerline = CreateCenterline(cylinder),
                        Diameter = cylinder.Radius * 2,
                        Confidence = cylinder.Confidence,
                        Material = ClassifyPipeMaterial(cylinder),
                        SystemType = InferSystemType(cylinder)
                    };
                    detectedPipes.Add(pipe);
                }
            }
            
            // Post-process to connect pipe segments
            return ConnectPipeSegments(detectedPipes);
        }

        /// <summary>
        /// Classifies MEP systems (HVAC ducts, piping, conduit, cable trays)
        /// </summary>
        public async Task<MEPClassification> ClassifyMEPSystems(List<DetectedObject> objects)
        {
            var classification = new MEPClassification();
            
            foreach (var obj in objects)
            {
                switch (await _mlService.ClassifyMEPObject(obj))
                {
                    case MEPType.HVACDuct:
                        // Rectangular patterns, larger cross-sections
                        classification.HVACDucts.Add(new DetectedDuct
                        {
                            Width = obj.BoundingBox.Width,
                            Height = obj.BoundingBox.Height,
                            Path = ExtractDuctPath(obj)
                        });
                        break;
                        
                    case MEPType.Piping:
                        // Cylindrical, various sizes
                        classification.Pipes.Add(ConvertToPipe(obj));
                        break;
                        
                    case MEPType.Conduit:
                        // Small cylinders, parallel runs
                        classification.Conduits.Add(new DetectedConduit
                        {
                            Diameter = obj.EstimatedDiameter,
                            Route = ExtractConduitRoute(obj),
                            IsFlexible = DetectFlexibility(obj)
                        });
                        break;
                        
                    case MEPType.CableTray:
                        // Ladder patterns
                        classification.CableTrays.Add(new DetectedCableTray
                        {
                            Width = obj.BoundingBox.Width,
                            Type = IdentifyCableTrayType(obj),
                            Route = ExtractTrayRoute(obj)
                        });
                        break;
                }
            }
            
            return classification;
        }

        /// <summary>
        /// Extracts structural elements from point cloud
        /// </summary>
        public async Task<List<StructuralElement>> ExtractStructuralElements(PointCloudInstance pointCloud)
        {
            var elements = new List<StructuralElement>();
            
            // Use plane detection for walls and slabs
            var planes = await _mlService.DetectPlanes(pointCloud);
            
            foreach (var plane in planes)
            {
                if (IsVerticalPlane(plane))
                {
                    // Potential wall or column
                    if (IsColumnLike(plane))
                    {
                        elements.Add(CreateColumnFromPlane(plane));
                    }
                    else
                    {
                        elements.Add(CreateWallFromPlane(plane));
                    }
                }
                else if (IsHorizontalPlane(plane))
                {
                    // Floor or ceiling
                    elements.Add(CreateSlabFromPlane(plane));
                }
                else
                {
                    // Potential beam
                    var beam = AnalyzeAsBeam(plane);
                    if (beam != null) elements.Add(beam);
                }
            }
            
            return elements;
        }

        /// <summary>
        /// Specialized method for underground pipe detection with depth analysis
        /// </summary>
        public async Task<List<UndergroundPipe>> DetectUndergroundPipes(
            PointCloudRegion region, 
            Surface groundSurface)
        {
            var pipes = await DetectPipes(region);
            var undergroundPipes = new List<UndergroundPipe>();
            
            foreach (var pipe in pipes)
            {
                var ugPipe = new UndergroundPipe
                {
                    Pipe = pipe,
                    BurialDepths = CalculateBurialDepths(pipe.Centerline, groundSurface),
                    Material = InferUndergroundMaterial(pipe),
                    Condition = AssessPipeCondition(pipe)
                };
                
                undergroundPipes.Add(ugPipe);
            }
            
            return undergroundPipes;
        }

        #region Helper Methods

        private List<double> CalculateBurialDepths(Line centerline, Surface ground)
        {
            var depths = new List<double>();
            const int samplePoints = 10;
            
            for (int i = 0; i <= samplePoints; i++)
            {
                var param = i / (double)samplePoints;
                var point = centerline.Evaluate(param, true);
                var groundElev = ground.Project(point).XYZPoint.Z;
                depths.Add(groundElev - point.Z);
            }
            
            return depths;
        }

        private bool IsPipeLikeCylinder(CylindricalObject cylinder)
        {
            // Check aspect ratio and size constraints
            var lengthToDiameterRatio = cylinder.Length / (cylinder.Radius * 2);
            return lengthToDiameterRatio > 3 && 
                   cylinder.Radius > 0.5 / 12 && // Min 0.5" radius
                   cylinder.Radius < 36.0 / 12;  // Max 36" radius
        }

        private List<DetectedPipe> ConnectPipeSegments(List<DetectedPipe> segments)
        {
            // Group segments that should be connected
            var connected = new List<DetectedPipe>();
            var processed = new HashSet<DetectedPipe>();
            
            foreach (var segment in segments)
            {
                if (processed.Contains(segment)) continue;
                
                var connectedSegment = segment;
                processed.Add(segment);
                
                // Find segments that align with this one
                var aligned = FindAlignedSegments(segment, segments, processed);
                
                if (aligned.Any())
                {
                    connectedSegment = MergeSegments(segment, aligned);
                    aligned.ForEach(s => processed.Add(s));
                }
                
                connected.Add(connectedSegment);
            }
            
            return connected;
        }

        private PipeMaterial ClassifyPipeMaterial(CylindricalObject cylinder)
        {
            // Use color, reflectivity, and context to infer material
            // This would integrate with ML material classification
            
            // Placeholder logic
            if (cylinder.AverageColor.Blue > 200) return PipeMaterial.PVC;
            if (cylinder.Reflectivity > 0.8) return PipeMaterial.StainlessSteel;
            if (cylinder.Radius > 8.0 / 12) return PipeMaterial.DuctileIron;
            
            return PipeMaterial.Unknown;
        }

        #endregion
    }

    #region Supporting Classes

    public class DetectedPipe
    {
        public Line Centerline { get; set; }
        public double Diameter { get; set; }
        public double Confidence { get; set; }
        public PipeMaterial Material { get; set; }
        public MEPSystemType SystemType { get; set; }
    }

    public class UndergroundPipe
    {
        public DetectedPipe Pipe { get; set; }
        public List<double> BurialDepths { get; set; }
        public PipeMaterial Material { get; set; }
        public PipeCondition Condition { get; set; }
    }

    public enum PipeMaterial
    {
        PVC, HDPE, DuctileIron, CastIron, Concrete, Clay, StainlessSteel, Copper, Unknown
    }

    public enum PipeCondition
    {
        Excellent, Good, Fair, Poor, Critical
    }

    #endregion
}
