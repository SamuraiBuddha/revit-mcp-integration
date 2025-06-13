using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitMcpServer.Models
{
    public class NetworkCreationSettings
    {
        public ElementId LevelId { get; set; }
        public string DefaultMaterial { get; set; }
        public bool AutoGenerateStructures { get; set; }
        public double StructureSpacing { get; set; }
        public Dictionary<string, string> MaterialMappings { get; set; }
    }
}
