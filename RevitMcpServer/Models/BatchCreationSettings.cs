namespace RevitMcpServer.Models
{
    public class BatchCreationSettings
    {
        public bool AutoConnect { get; set; }
        public bool CreateStructures { get; set; }
        public double StructureSpacing { get; set; }
        public string DefaultMaterial { get; set; }
        public bool ValidateNetworkTopology { get; set; }
        public int BatchSize { get; set; } = 100;
    }
}
