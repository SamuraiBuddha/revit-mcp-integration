using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

namespace RevitMcpServer.Models
{
    /// <summary>
    /// Represents a region of point cloud data for analysis
    /// </summary>
    public class PointCloudRegion
    {
        public string Id { get; set; }
        public BoundingBoxXYZ Bounds { get; set; }
        public List<XYZ> Points { get; set; }
        public int PointCount => Points?.Count ?? 0;
        public string Name { get; set; }
        
        public PointCloudRegion()
        {
            Points = new List<XYZ>();
            Id = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Represents ground surface data for underground utility detection
    /// </summary>
    public class GroundSurface
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<XYZ> SurfacePoints { get; set; }
        public double AverageElevation { get; set; }
        public BoundingBoxXYZ Bounds { get; set; }
        
        public GroundSurface()
        {
            SurfacePoints = new List<XYZ>();
        }
        
        public double GetElevationAt(XYZ point)
        {
            // Simplified implementation - would use triangulation in real scenario
            return AverageElevation;
        }
    }

    /// <summary>
    /// Settings for creating pipes from scan data
    /// </summary>
    public class PipeCreationSettings
    {
        public Level ReferenceLevel { get; set; }
        public bool AutoSizeEnabled { get; set; } = true;
        public bool AutoConnectEnabled { get; set; } = true;
        public double MinimumDiameter { get; set; } = 0.5; // feet
        public double MaximumDiameter { get; set; } = 10.0; // feet
        public PipingSystemType DefaultSystemType { get; set; }
        public string DefaultMaterial { get; set; } = "Standard";
    }

    /// <summary>
    /// Analysis result for pipe intersections
    /// </summary>
    public class IntersectionAnalysis
    {
        public Pipe Pipe1 { get; set; }
        public Pipe Pipe2 { get; set; }
        public XYZ IntersectionPoint { get; set; }
        public IntersectionType Type { get; set; }
        public double Angle { get; set; }
        public bool RequiresFitting { get; set; }
        public string RecommendedFittingType { get; set; }
        public List<Pipe> AllPipes { get; set; }
        public bool IsGravitySystem { get; set; }
        
        public enum IntersectionType
        {
            Tee,
            Elbow,
            Cross,
            Wye,
            Reducer,
            Unknown
        }
    }

    /// <summary>
    /// MEP system classification result - corrected name
    /// </summary>
    public class MEPClassification
    {
        public List<DetectedMEPElement> HVACDucts { get; set; }
        public List<DetectedMEPElement> Pipes { get; set; }
        public List<DetectedMEPElement> Conduits { get; set; }
        public List<DetectedMEPElement> CableTrays { get; set; }
        public double OverallConfidence { get; set; }
        
        public MEPClassification()
        {
            HVACDucts = new List<DetectedMEPElement>();
            Pipes = new List<DetectedMEPElement>();
            Conduits = new List<DetectedMEPElement>();
            CableTrays = new List<DetectedMEPElement>();
        }
    }

    /// <summary>
    /// Represents an underground pipe with depth information
    /// </summary>
    public class UndergroundPipe
    {
        public DetectedPipe Pipe { get; set; }
        public List<double> BurialDepths { get; set; }
        public PipeMaterial Material { get; set; }
        public PipeCondition Condition { get; set; }
        public DateTime? InstallationDate { get; set; }
        public string Owner { get; set; }
        
        public UndergroundPipe()
        {
            BurialDepths = new List<double>();
            Condition = PipeCondition.Unknown;
        }
    }

    /// <summary>
    /// Pipe material enumeration
    /// </summary>
    public enum PipeMaterial
    {
        PVC,
        HDPE,
        Steel,
        CastIron,
        Concrete,
        Clay,
        Copper,
        Unknown
    }

    /// <summary>
    /// Pipe condition enumeration
    /// </summary>
    public enum PipeCondition
    {
        Excellent,
        Good,
        Fair,
        Poor,
        Critical,
        Unknown
    }

    /// <summary>
    /// ML Service response for detected objects
    /// </summary>
    public class DetectedObject
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public BoundingBoxXYZ Bounds { get; set; }
        public double Confidence { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        
        public DetectedObject()
        {
            Id = Guid.NewGuid().ToString();
            Properties = new Dictionary<string, object>();
        }
    }
}
