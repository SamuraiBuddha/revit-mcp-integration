using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitMcpServer.Models
{
    public class UndergroundUtilityData
    {
        public ElementId ElementId { get; set; }
        public UtilityType UtilityType { get; set; }
        public double Diameter { get; set; }
        public string Material { get; set; }
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public double BurialDepth { get; set; }
        public double Confidence { get; set; }
        public Dictionary<string, object> AdditionalProperties { get; set; }
    }

    public enum UtilityType
    {
        WaterMain,
        SanitarySewer,
        StormSewer,
        GasMain,
        Electrical,
        Telecom,
        Unknown
    }
}
