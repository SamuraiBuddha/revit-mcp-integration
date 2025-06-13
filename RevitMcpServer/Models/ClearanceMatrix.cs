using System.Collections.Generic;

namespace RevitMcpServer.Models
{
    public class ClearanceMatrix
    {
        private Dictionary<(string, string), double> _clearances = new Dictionary<(string, string), double>();

        public ClearanceMatrix()
        {
            // Initialize with standard clearances (in feet)
            SetClearance("Water Main", "Sanitary Sewer", 10.0);
            SetClearance("Water Main", "Storm Sewer", 10.0);
            SetClearance("Gas Main", "Electrical", 12.0);
            SetClearance("Gas Main", "Water Main", 10.0);
            SetClearance("Electrical", "Telecom", 12.0);
        }

        public void SetClearance(string utility1, string utility2, double clearanceFeet)
        {
            _clearances[(utility1, utility2)] = clearanceFeet;
            _clearances[(utility2, utility1)] = clearanceFeet;
        }

        public double GetClearance(string utility1, string utility2)
        {
            if (_clearances.TryGetValue((utility1, utility2), out var clearance))
                return clearance;
            return 2.0; // Default clearance
        }
    }
}
