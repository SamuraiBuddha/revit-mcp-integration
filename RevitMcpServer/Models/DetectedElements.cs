using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitMcpServer.Models
{
    /// <summary>
    /// Represents a detected pipe from scan data
    /// </summary>
    public class DetectedPipe
    {
        public string Id { get; set; }
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public double Diameter { get; set; }
        public Line Centerline { get; set; }
        public MEPSystemType SystemType { get; set; }
        public double Confidence { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        
        public DetectedPipe()
        {
            Id = Guid.NewGuid().ToString();
            Properties = new Dictionary<string, object>();
            Confidence = 0.0;
        }
    }

    /// <summary>
    /// Represents a detected MEP element from scan data
    /// </summary>
    public class DetectedMEPElement
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public BoundingBoxXYZ Bounds { get; set; }
        public double Confidence { get; set; }
        public MEPSystemType SystemType { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        
        public DetectedMEPElement()
        {
            Id = Guid.NewGuid().ToString();
            Properties = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// MEP System type enumeration to avoid ambiguity with Revit's MEPSystemType
    /// </summary>
    public enum MEPSystemType
    {
        SupplyAir,
        ReturnAir,
        ExhaustAir,
        DomesticColdWater,
        DomesticHotWater,
        SanitaryWaste,
        StormDrainage,
        FireProtection,
        Electrical,
        Data,
        Unknown
    }
}