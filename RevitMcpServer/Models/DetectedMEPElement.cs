using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitMcpServer.Models
{
    public class DetectedMEPElement
    {
        public string Id { get; set; }
        public string ElementType { get; set; }
        public XYZ Location { get; set; }
        public BoundingBoxXYZ BoundingBox { get; set; }
        public double Confidence { get; set; }
        public string SystemType { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }
}
