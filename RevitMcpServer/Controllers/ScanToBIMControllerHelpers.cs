using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using Autodesk.Revit.UI;
using RevitMcpServer.Models;

namespace RevitMcpServer.Controllers
{
    /// <summary>
    /// Helper methods and extensions for ScanToBIMController
    /// </summary>
    public partial class ScanToBIMController
    {
        private readonly Dictionary<string, List<DetectedObject>> _scanDataCache = new Dictionary<string, List<DetectedObject>>();

        /// <summary>
        /// Gets detected objects from a scan data ID
        /// </summary>
        private List<DetectedObject> GetDetectedObjects(string scanDataId)
        {
            // Check cache first
            if (_scanDataCache.ContainsKey(scanDataId))
            {
                return _scanDataCache[scanDataId];
            }

            // In a real implementation, this would retrieve from a database or file
            // For now, return empty list
            return new List<DetectedObject>();
        }

        /// <summary>
        /// Gets ground surface by ID
        /// </summary>
        private GroundSurface GetGroundSurface(int groundSurfaceId)
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            
            // In a real implementation, this would retrieve from stored surfaces
            // For now, create a simple ground surface
            var groundSurface = new GroundSurface
            {
                Id = groundSurfaceId,
                Name = $"Ground Surface {groundSurfaceId}",
                AverageElevation = 0.0
            };

            // Try to find topography surface
            var topoCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(TopographySurface))
                .Cast<TopographySurface>()
                .FirstOrDefault();

            if (topoCollector != null)
            {
                var points = topoCollector.GetPoints();
                groundSurface.SurfacePoints = points.Select(p => p.Position).ToList();
                groundSurface.AverageElevation = points.Average(p => p.Position.Z);
                groundSurface.Bounds = topoCollector.get_BoundingBox(null);
            }

            return groundSurface;
        }

        /// <summary>
        /// Gets pipes by their element IDs
        /// </summary>
        private List<Pipe> GetPipesByIds(List<int> pipeIds)
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            var pipes = new List<Pipe>();

            foreach (var id in pipeIds)
            {
                var elementId = new ElementId(id);
                var element = doc.GetElement(elementId);
                
                if (element is Pipe pipe)
                {
                    pipes.Add(pipe);
                }
            }

            return pipes;
        }

        /// <summary>
        /// Analyzes the intersection between two pipes
        /// </summary>
        private IntersectionAnalysis AnalyzePipeIntersection(Pipe pipe1, Pipe pipe2)
        {
            try
            {
                // Get pipe curves
                var curve1 = (pipe1.Location as LocationCurve)?.Curve;
                var curve2 = (pipe2.Location as LocationCurve)?.Curve;

                if (curve1 == null || curve2 == null)
                    return null;

                // Check for intersection
                var result = curve1.Intersect(curve2, out IntersectionResultArray results);
                
                if (result != SetComparisonResult.Overlap || results == null || results.Size == 0)
                    return null;

                var intersectionPoint = results.get_Item(0).XYZPoint;

                // Calculate angle between pipes
                var dir1 = (curve1.GetEndPoint(1) - curve1.GetEndPoint(0)).Normalize();
                var dir2 = (curve2.GetEndPoint(1) - curve2.GetEndPoint(0)).Normalize();
                var angle = Math.Acos(Math.Abs(dir1.DotProduct(dir2))) * 180 / Math.PI;

                // Determine intersection type
                var intersectionType = DetermineIntersectionType(angle);

                return new IntersectionAnalysis
                {
                    Pipe1 = pipe1,
                    Pipe2 = pipe2,
                    IntersectionPoint = intersectionPoint,
                    Type = intersectionType,
                    Angle = angle,
                    RequiresFitting = true,
                    RecommendedFittingType = GetRecommendedFitting(intersectionType, pipe1, pipe2)
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Determines the type of intersection based on angle
        /// </summary>
        private IntersectionAnalysis.IntersectionType DetermineIntersectionType(double angle)
        {
            if (Math.Abs(angle - 90) < 5)
                return IntersectionAnalysis.IntersectionType.Tee;
            else if (Math.Abs(angle - 45) < 5)
                return IntersectionAnalysis.IntersectionType.Wye;
            else if (angle < 15)
                return IntersectionAnalysis.IntersectionType.Reducer;
            else if (Math.Abs(angle - 180) < 15)
                return IntersectionAnalysis.IntersectionType.Elbow;
            else
                return IntersectionAnalysis.IntersectionType.Unknown;
        }

        /// <summary>
        /// Gets recommended fitting type based on intersection
        /// </summary>
        private string GetRecommendedFitting(IntersectionAnalysis.IntersectionType type, Pipe pipe1, Pipe pipe2)
        {
            var diameter1 = pipe1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.AsDouble() ?? 0;
            var diameter2 = pipe2.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.AsDouble() ?? 0;

            switch (type)
            {
                case IntersectionAnalysis.IntersectionType.Tee:
                    return diameter1 == diameter2 ? "Tee - Equal" : "Tee - Reducing";
                case IntersectionAnalysis.IntersectionType.Elbow:
                    return "90 Degree Elbow";
                case IntersectionAnalysis.IntersectionType.Wye:
                    return "45 Degree Wye";
                case IntersectionAnalysis.IntersectionType.Reducer:
                    return "Concentric Reducer";
                default:
                    return "Generic Fitting";
            }
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        private void LogWarning(string message)
        {
            // In a real implementation, this would write to a log file or service
            TaskDialog.Show("Warning", message);
        }
    }

    /// <summary>
    /// ML Service interface
    /// </summary>
    public interface IMLService
    {
        Task<List<DetectedPipe>> DetectPipes(PointCloudRegion region, double confidenceThreshold);
        Task<MLEPClassification> ClassifyMEPElements(List<DetectedObject> objects);
        Task<List<UndergroundPipe>> DetectUndergroundPipes(PointCloudRegion region, GroundSurface surface);
    }

    /// <summary>
    /// Mock ML Service implementation
    /// </summary>
    public class MLService : IMLService
    {
        public async Task<List<DetectedPipe>> DetectPipes(PointCloudRegion region, double confidenceThreshold)
        {
            // Simulate ML processing
            await Task.Delay(100);
            
            // Return mock detected pipes
            return new List<DetectedPipe>
            {
                new DetectedPipe
                {
                    Confidence = 0.95,
                    Diameter = 0.5, // feet
                    Material = PipeMaterial.Steel,
                    SystemType = MEPSystemType.SupplyHydronic,
                    Centerline = new CylindricalCenterline
                    {
                        StartPoint = new XYZ(0, 0, 0),
                        EndPoint = new XYZ(10, 0, 0),
                        Radius = 0.25
                    }
                }
            };
        }

        public async Task<MLEPClassification> ClassifyMEPElements(List<DetectedObject> objects)
        {
            // Simulate ML processing
            await Task.Delay(100);

            var classification = new MLEPClassification
            {
                OverallConfidence = 0.92
            };

            // Mock classification
            foreach (var obj in objects)
            {
                var mepElement = new DetectedMEPElement
                {
                    BoundingBox = obj.Bounds,
                    Confidence = obj.Confidence,
                    Type = obj.Type
                };

                switch (obj.Type?.ToLower())
                {
                    case "duct":
                        classification.HVACDucts.Add(mepElement);
                        break;
                    case "pipe":
                        classification.Pipes.Add(mepElement);
                        break;
                    case "conduit":
                        classification.Conduits.Add(mepElement);
                        break;
                    case "cabletray":
                        classification.CableTrays.Add(mepElement);
                        break;
                }
            }

            return classification;
        }

        public async Task<List<UndergroundPipe>> DetectUndergroundPipes(PointCloudRegion region, GroundSurface surface)
        {
            // Simulate ML processing
            await Task.Delay(100);

            // Return mock underground pipes
            return new List<UndergroundPipe>
            {
                new UndergroundPipe
                {
                    Pipe = new DetectedPipe
                    {
                        Confidence = 0.88,
                        Diameter = 1.0, // feet
                        Material = PipeMaterial.PVC,
                        SystemType = MEPSystemType.DomesticColdWater,
                        Centerline = new CylindricalCenterline
                        {
                            StartPoint = new XYZ(0, 0, -5),
                            EndPoint = new XYZ(20, 0, -5),
                            Radius = 0.5
                        }
                    },
                    BurialDepths = new List<double> { 4.5, 5.0, 5.5 },
                    Material = PipeMaterial.PVC,
                    Condition = PipeCondition.Good,
                    Owner = "City Water Department"
                }
            };
        }
    }

    /// <summary>
    /// Cylindrical centerline representation
    /// </summary>
    public class CylindricalCenterline
    {
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public double Radius { get; set; }
        
        public double Length => StartPoint.DistanceTo(EndPoint);
    }
}
