using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitMcpServer.Models
{
    public class DetectedPipe
    {
        public string Id { get; set; }
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public double Diameter { get; set; }
        public string SystemType { get; set; }
        public double Confidence { get; set; }
        public List<XYZ> CenterlinePoints { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }
}
