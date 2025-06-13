using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitMcpServer.Models
{
    public class DepthAnalysisResult
    {
        public List<PipeDepthInfo> PipeDepths { get; set; }
    }

    public class PipeDepthInfo
    {
        public ElementId ElementId { get; set; }
        public UtilityType SystemType { get; set; }
        public double MinDepth { get; set; }
        public double MaxDepth { get; set; }
        public double AverageDepth { get; set; }
        public List<DepthViolation> DepthViolations { get; set; }
    }

    public class DepthViolation
    {
        public string Description { get; set; }
        public double ActualDepth { get; set; }
        public double RequiredDepth { get; set; }
    }

    public class UtilityCorridor
    {
        public List<Element> Utilities { get; set; }
        public double Width { get; set; }
        public Curve Alignment { get; set; }
        public List<ClearanceZone> ClearanceZones { get; set; }
    }

    public class ClearanceZone
    {
        public string Type { get; set; }
        public BoundingBoxXYZ Bounds { get; set; }
    }

    public class InvertData
    {
        public ElementId PipeId { get; set; }
        public double UpstreamInvert { get; set; }
        public double DownstreamInvert { get; set; }
        public double Slope { get; set; }
        public FlowDirection FlowDirection { get; set; }
    }

    public enum FlowDirection
    {
        Upstream,
        Downstream,
        Bidirectional
    }

    public class UndergroundClash
    {
        public Element ExistingUtility { get; set; }
        public Element ProposedUtility { get; set; }
        public ClashType ClashType { get; set; }
        public ClashSeverity Severity { get; set; }
        public double RequiredClearance { get; set; }
        public double ActualClearance { get; set; }
        public XYZ Location { get; set; }
    }

    public enum ClashType
    {
        HardClash,
        ClearanceViolation,
        Crossing
    }

    public enum ClashSeverity
    {
        Critical,
        Major,
        Minor,
        Warning
    }

    public class UndergroundNetwork
    {
        public List<Element> Pipes { get; set; }
        public List<FamilyInstance> Structures { get; set; }
        public List<string> Errors { get; set; }
    }

    public class GPRIntegrationResult
    {
        public List<VerifiedUtility> VerifiedUtilities { get; set; }
        public List<GPRUtility> GPROnlyUtilities { get; set; }
    }

    public class VerifiedUtility
    {
        public UtilityType Type { get; set; }
        public double Depth { get; set; }
        public double Confidence { get; set; }
    }

    public class GPRUtility
    {
        public UtilityType UtilityType { get; set; }
        public double Depth { get; set; }
        public double Confidence { get; set; }
    }
}
