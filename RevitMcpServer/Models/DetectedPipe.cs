using Autodesk.Revit.DB;
using System.Collections.Generic;
using RevitMcpServer.Controllers;

namespace RevitMcpServer.Models
{
    public class DetectedPipe
    {
        public string Id { get; set; }
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public double Diameter { get; set; }
        public MEPSystemType SystemType { get; set; }
        public PipeMaterial Material { get; set; }
        public double Confidence { get; set; }
        public List<XYZ> CenterlinePoints { get; set; }
        public CylindricalCenterline Centerline { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        
        public DetectedPipe()
        {
            CenterlinePoints = new List<XYZ>();
            Properties = new Dictionary<string, object>();
            Material = PipeMaterial.Unknown;
            SystemType = MEPSystemType.Unknown;
        }
    }
    
    /// <summary>
    /// MEP system types for scan detection
    /// </summary>
    public enum MEPSystemType
    {
        Unknown,
        DomesticColdWater,
        DomesticHotWater,
        SupplyHydronic,
        ReturnHydronic,
        FireProtectionWet,
        FireProtectionDry,
        SanitaryWaste,
        StormDrainage,
        VentPiping,
        Steam,
        Condensate,
        NaturalGas,
        CompressedAir,
        SupplyAir,
        ReturnAir,
        ExhaustAir,
        PowerElectrical,
        LightingElectrical,
        DataCommunication
    }
}
