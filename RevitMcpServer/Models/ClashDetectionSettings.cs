using System.Collections.Generic;

namespace RevitMcpServer.Models
{
    public class ClashDetectionSettings
    {
        public bool CheckClearances { get; set; }
        public ClearanceMatrix ClearanceMatrix { get; set; }
        public bool IncludeCrossings { get; set; }
        public double ToleranceFeet { get; set; } = 0.1;
    }
}
