using System.Collections.Generic;

namespace RevitMcpServer.Models
{
    public class CorridorSettings
    {
        public double GroupingDistance { get; set; }
        public bool IncludeExisting { get; set; }
        public List<ClearanceStandard> ClearanceStandards { get; set; }
    }

    public class ClearanceStandard
    {
        public string Name { get; set; }
        public string UtilityType1 { get; set; }
        public string UtilityType2 { get; set; }
        public double RequiredClearance { get; set; }
    }
}
