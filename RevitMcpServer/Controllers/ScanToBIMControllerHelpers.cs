using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using RevitMcpServer.Models;
using RevitMcpServer.ScanToBIM;

namespace RevitMcpServer.Controllers
{
    /// <summary>
    /// Helper methods for ScanToBIMController
    /// </summary>
    public partial class ScanToBIMController
    {
        private List<DetectedObject> GetDetectedObjects(string scanDataId)
        {
            // Mock implementation - would retrieve from scan data service
            return new List<DetectedObject>();
        }

        private GroundSurface GetGroundSurface(int groundSurfaceId)
        {
            // Mock implementation - would retrieve from database
            return new GroundSurface
            {
                Id = groundSurfaceId,
                Name = "Ground Surface",
                AverageElevation = 100.0
            };
        }

        private List<Pipe> GetPipesByIds(List<int> pipeIds)
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            var pipes = new List<Pipe>();
            
            foreach (var id in pipeIds)
            {
                var element = doc.GetElement(new ElementId(id));
                if (element is Pipe pipe)
                {
                    pipes.Add(pipe);
                }
            }
            
            return pipes;
        }

        private RevitMcpServer.Models.IntersectionAnalysis AnalyzePipeIntersection(Pipe pipe1, Pipe pipe2)
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
            
            var intersection = results.get_Item(0);
            
            // Calculate angle between pipes
            var dir1 = (curve1 as Line)?.Direction ?? XYZ.Zero;
            var dir2 = (curve2 as Line)?.Direction ?? XYZ.Zero;
            var angle = Math.Acos(dir1.DotProduct(dir2)) * 180 / Math.PI;
            
            return new RevitMcpServer.Models.IntersectionAnalysis
            {
                Pipe1 = pipe1,
                Pipe2 = pipe2,
                IntersectionPoint = intersection.XYZPoint,
                Angle = angle,
                RequiresFitting = true,
                AllPipes = new List<Pipe> { pipe1, pipe2 }
            };
        }

        private void LogWarning(string message)
        {
            // Implementation would log to service
            Console.WriteLine($"WARNING: {message}");
        }
    }
}
