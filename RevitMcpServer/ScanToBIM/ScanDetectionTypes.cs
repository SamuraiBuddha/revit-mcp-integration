using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitMcpServer.ScanToBIM
{
    /// <summary>
    /// Represents a cylindrical object detected in point cloud data
    /// </summary>
    public class CylindricalObject
    {
        public string Id { get; set; }
        public XYZ Center { get; set; }
        public XYZ Direction { get; set; }
        public double Radius { get; set; }
        public double Length { get; set; }
        public double Confidence { get; set; }
        public List<XYZ> PointCloud { get; set; }
        public CylindricalCenterline Centerline { get; set; }
        
        public CylindricalObject()
        {
            Id = Guid.NewGuid().ToString();
            PointCloud = new List<XYZ>();
            Confidence = 0.0;
        }
        
        /// <summary>
        /// Calculates the volume of the cylindrical object
        /// </summary>
        public double GetVolume()
        {
            return Math.PI * Radius * Radius * Length;
        }
        
        /// <summary>
        /// Gets the bounding box of the cylindrical object
        /// </summary>
        public BoundingBoxXYZ GetBoundingBox()
        {
            var bb = new BoundingBoxXYZ();
            var transform = Transform.Identity;
            
            // Calculate endpoints
            var halfLength = Length / 2.0;
            var startPoint = Center - Direction * halfLength;
            var endPoint = Center + Direction * halfLength;
            
            // Expand by radius in all directions
            bb.Min = new XYZ(
                Math.Min(startPoint.X, endPoint.X) - Radius,
                Math.Min(startPoint.Y, endPoint.Y) - Radius,
                Math.Min(startPoint.Z, endPoint.Z) - Radius
            );
            
            bb.Max = new XYZ(
                Math.Max(startPoint.X, endPoint.X) + Radius,
                Math.Max(startPoint.Y, endPoint.Y) + Radius,
                Math.Max(startPoint.Z, endPoint.Z) + Radius
            );
            
            bb.Transform = transform;
            return bb;
        }
    }
    
    /// <summary>
    /// Represents the centerline of a cylindrical object
    /// </summary>
    public class CylindricalCenterline
    {
        public Line Line { get; set; }
        public List<XYZ> Points { get; set; }
        public double AverageRadius { get; set; }
        
        public CylindricalCenterline()
        {
            Points = new List<XYZ>();
        }
        
        public XYZ GetStartPoint()
        {
            return Line?.GetEndPoint(0) ?? (Points.Count > 0 ? Points[0] : XYZ.Zero);
        }
        
        public XYZ GetEndPoint()
        {
            return Line?.GetEndPoint(1) ?? (Points.Count > 0 ? Points[Points.Count - 1] : XYZ.Zero);
        }
        
        public double GetLength()
        {
            if (Line != null)
                return Line.Length;
                
            if (Points.Count < 2)
                return 0.0;
                
            double totalLength = 0.0;
            for (int i = 1; i < Points.Count; i++)
            {
                totalLength += Points[i].DistanceTo(Points[i - 1]);
            }
            return totalLength;
        }
    }
    
    /// <summary>
    /// Represents a structural element detected in point cloud data
    /// </summary>
    public class StructuralElement
    {
        public string Id { get; set; }
        public string Type { get; set; } // Column, Beam, Wall, Floor, etc.
        public BoundingBoxXYZ BoundingBox { get; set; }
        public List<XYZ> PointCloud { get; set; }
        public double Confidence { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        
        // Geometric properties
        public XYZ Location { get; set; }
        public XYZ Direction { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Length { get; set; }
        
        public StructuralElement()
        {
            Id = Guid.NewGuid().ToString();
            PointCloud = new List<XYZ>();
            Properties = new Dictionary<string, object>();
            Type = "Unknown";
        }
        
        /// <summary>
        /// Determines if this is a vertical element (column)
        /// </summary>
        public bool IsVertical()
        {
            if (Direction == null) return false;
            
            var verticalVector = XYZ.BasisZ;
            var angle = Math.Acos(Direction.DotProduct(verticalVector));
            return angle < Math.PI / 6; // Within 30 degrees of vertical
        }
        
        /// <summary>
        /// Determines if this is a horizontal element (beam/floor)
        /// </summary>
        public bool IsHorizontal()
        {
            if (Direction == null) return false;
            
            var verticalVector = XYZ.BasisZ;
            var angle = Math.Acos(Direction.DotProduct(verticalVector));
            return Math.Abs(angle - Math.PI / 2) < Math.PI / 6; // Within 30 degrees of horizontal
        }
        
        /// <summary>
        /// Gets the cross-sectional area
        /// </summary>
        public double GetCrossSectionalArea()
        {
            switch (Type.ToLower())
            {
                case "column":
                case "beam":
                    return Width * Height;
                case "wall":
                    return Width * Length;
                case "floor":
                    return Width * Height;
                default:
                    return 0.0;
            }
        }
    }
}
