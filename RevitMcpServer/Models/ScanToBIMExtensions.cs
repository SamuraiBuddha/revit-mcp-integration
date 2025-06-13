using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitMcpServer.Models
{
    /// <summary>
    /// Detected MEP element from scan data
    /// </summary>
    public class DetectedMEPElement
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public BoundingBoxXYZ BoundingBox { get; set; }
        public double Confidence { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Diameter { get; set; }
        public List<XYZ> Route { get; set; }
        public List<XYZ> Path { get; set; }
        public bool IsFlexible { get; set; }
        
        public DetectedMEPElement()
        {
            Route = new List<XYZ>();
            Path = new List<XYZ>();
        }
    }

    /// <summary>
    /// Structural element detected from scan
    /// </summary>
    public class StructuralElement
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public BoundingBoxXYZ Bounds { get; set; }
        public double Confidence { get; set; }
    }

    /// <summary>
    /// Underground utility data
    /// </summary>
    public class UndergroundUtilityData
    {
        public string Id { get; set; }
        public List<PipeSegment> Segments { get; set; }
        public double BurialDepth { get; set; }
        public PipeMaterial Material { get; set; }
        public string PipeClass { get; set; }
        public int? InstallationYear { get; set; }
        public List<StructureLocation> StructureLocations { get; set; }
        
        public UndergroundUtilityData()
        {
            Segments = new List<PipeSegment>();
            StructureLocations = new List<StructureLocation>();
        }
    }

    /// <summary>
    /// Pipe segment data
    /// </summary>
    public class PipeSegment
    {
        public XYZ Start { get; set; }
        public XYZ End { get; set; }
        public double Diameter { get; set; }
    }

    /// <summary>
    /// Structure location data
    /// </summary>
    public class StructureLocation
    {
        public XYZ Point { get; set; }
        public double RimElevation { get; set; }
        public string Type { get; set; }
    }

    /// <summary>
    /// Batch creation settings
    /// </summary>
    public class BatchCreationSettings
    {
        public Level UndergroundLevel { get; set; }
        public bool AutoConnect { get; set; } = true;
        public bool CreateStructures { get; set; } = true;
        public double MaxStructureSpacing { get; set; } = 300.0; // feet
    }

    /// <summary>
    /// Detected object with estimated diameter
    /// </summary>
    public partial class DetectedObject
    {
        public double EstimatedDiameter { get; set; }
    }
}