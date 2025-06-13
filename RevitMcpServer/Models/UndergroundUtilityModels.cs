using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

namespace RevitMcpServer.Models
{
    /// <summary>
    /// Result of creating an underground utility network
    /// </summary>
    public class UtilityNetworkResult
    {
        public List<Pipe> CreatedPipes { get; set; } = new List<Pipe>();
        public List<FamilyInstance> CreatedStructures { get; set; } = new List<FamilyInstance>();
        public List<CreationError> Errors { get; set; } = new List<CreationError>();
        public int TotalElements => CreatedPipes.Count + CreatedStructures.Count;
        public bool HasErrors => Errors.Count > 0;
    }

    /// <summary>
    /// Error information for creation failures
    /// </summary>
    public class CreationError
    {
        public string Message { get; set; }
        public DetectedPipe DetectedPipe { get; set; }
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// Depth analysis result for utility elements
    /// </summary>
    public class DepthAnalysisResult
    {
        public List<PipeDepthInfo> PipeDepths { get; set; }
        public double MinimumDepth => PipeDepths?.Count > 0 ? PipeDepths.Min(p => p.MinDepth) : 0;
        public double MaximumDepth => PipeDepths?.Count > 0 ? PipeDepths.Max(p => p.MaxDepth) : 0;
    }

    /// <summary>
    /// Depth information for a single pipe
    /// </summary>
    public class PipeDepthInfo
    {
        public ElementId ElementId { get; set; }
        public UtilityType SystemType { get; set; }
        public double MinDepth { get; set; }
        public double MaxDepth { get; set; }
        public double AverageDepth { get; set; }
        public List<DepthViolation> DepthViolations { get; set; }
    }

    /// <summary>
    /// Represents a depth requirement violation
    /// </summary>
    public class DepthViolation
    {
        public string Description { get; set; }
        public double ActualDepth { get; set; }
        public double RequiredDepth { get; set; }
        public XYZ Location { get; set; }
    }

    /// <summary>
    /// Utility corridor for grouping related utilities
    /// </summary>
    public class UtilityCorridor
    {
        public List<Element> Utilities { get; set; }
        public Curve Alignment { get; set; }
        public double Width { get; set; }
        public List<ClearanceZone> ClearanceZones { get; set; }
    }

    /// <summary>
    /// Clearance zone around utilities
    /// </summary>
    public class ClearanceZone
    {
        public string Name { get; set; }
        public double RequiredClearance { get; set; }
        public BoundingBoxXYZ Bounds { get; set; }
    }

    /// <summary>
    /// Settings for corridor generation
    /// </summary>
    public class CorridorSettings
    {
        public double GroupingDistance { get; set; } = 10.0; // feet
        public double MinimumCorridorWidth { get; set; } = 5.0; // feet
        public bool CreateClearanceZones { get; set; } = true;
    }

    /// <summary>
    /// Underground clash detection result
    /// </summary>
    public class UndergroundClash
    {
        public Element ExistingUtility { get; set; }
        public Element ProposedUtility { get; set; }
        public ClashType ClashType { get; set; }
        public double RequiredClearance { get; set; }
        public double ActualClearance { get; set; }
        public XYZ Location { get; set; }
        public ClashSeverity Severity { get; set; }
    }

    /// <summary>
    /// Types of utility clashes
    /// </summary>
    public enum ClashType
    {
        HardClash,
        ClearanceViolation,
        Crossing,
        Parallel,
        Unknown
    }

    /// <summary>
    /// Severity levels for clashes
    /// </summary>
    public enum ClashSeverity
    {
        Warning,
        Minor,
        Major,
        Critical
    }

    /// <summary>
    /// Settings for clash detection
    /// </summary>
    public class ClashDetectionSettings
    {
        public ClearanceMatrix ClearanceMatrix { get; set; }
        public double DefaultClearance { get; set; } = 2.0; // feet
        public bool CheckVerticalClearance { get; set; } = true;
        public bool CheckHorizontalClearance { get; set; } = true;
    }

    /// <summary>
    /// Matrix defining required clearances between utility types
    /// </summary>
    public class ClearanceMatrix
    {
        private Dictionary<string, Dictionary<string, double>> _matrix;

        public ClearanceMatrix()
        {
            _matrix = new Dictionary<string, Dictionary<string, double>>();
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            // Set default clearances between different utility types
            SetClearance("Water", "Sewer", 10.0);
            SetClearance("Gas", "Electric", 12.0);
            SetClearance("Electric", "Communication", 2.0);
        }

        public void SetClearance(string type1, string type2, double clearance)
        {
            if (!_matrix.ContainsKey(type1))
                _matrix[type1] = new Dictionary<string, double>();
            if (!_matrix.ContainsKey(type2))
                _matrix[type2] = new Dictionary<string, double>();

            _matrix[type1][type2] = clearance;
            _matrix[type2][type1] = clearance;
        }

        public double GetClearance(string type1, string type2)
        {
            if (_matrix.ContainsKey(type1) && _matrix[type1].ContainsKey(type2))
                return _matrix[type1][type2];
            return 2.0; // Default clearance
        }
    }

    /// <summary>
    /// Utility types for underground systems
    /// </summary>
    public enum UtilityType
    {
        WaterMain,
        SanitarySewer,
        StormSewer,
        Gas,
        Electric,
        Telecom,
        Unknown
    }

    /// <summary>
    /// Flow direction in gravity systems
    /// </summary>
    public enum FlowDirection
    {
        Upstream,
        Downstream,
        Bidirectional
    }

    /// <summary>
    /// Invert data for gravity pipe systems
    /// </summary>
    public class InvertData
    {
        public ElementId PipeId { get; set; }
        public double UpstreamInvert { get; set; }
        public double DownstreamInvert { get; set; }
        public double Slope { get; set; }
        public FlowDirection FlowDirection { get; set; }
    }

    /// <summary>
    /// GPR integration result
    /// </summary>
    public class GPRIntegrationResult
    {
        public List<VerifiedUtility> VerifiedUtilities { get; set; }
        public List<GPRUtility> GPROnlyUtilities { get; set; }
        public double OverallConfidence { get; set; }
    }

    /// <summary>
    /// Utility verified by GPR
    /// </summary>
    public class VerifiedUtility
    {
        public Element RevitElement { get; set; }
        public double GPRConfidence { get; set; }
        public XYZ GPRLocation { get; set; }
        public double LocationDifference { get; set; }
    }

    /// <summary>
    /// Utility detected only by GPR
    /// </summary>
    public class GPRUtility
    {
        public XYZ Location { get; set; }
        public double Depth { get; set; }
        public double Size { get; set; }
        public string ProbableType { get; set; }
        public double Confidence { get; set; }
    }
}