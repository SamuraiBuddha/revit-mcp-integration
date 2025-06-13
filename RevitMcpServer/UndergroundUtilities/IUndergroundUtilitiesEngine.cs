using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Plumbing;
using RevitMcpServer.Models;
using RevitMcpServer.ScanToBIM;

namespace RevitMcpServer.UndergroundUtilities
{
    /// <summary>
    /// Interface for underground utilities operations
    /// </summary>
    public interface IUndergroundUtilitiesEngine
    {
        /// <summary>
        /// Extracts invert elevations from pipes
        /// </summary>
        Task<Dictionary<Pipe, double>> ExtractInvertElevations(List<Pipe> pipes);

        /// <summary>
        /// Creates underground utility network
        /// </summary>
        Task<UtilityNetworkResult> CreateUndergroundNetwork(List<DetectedPipe> detectedPipes, NetworkCreationSettings settings);
    }

    /// <summary>
    /// Network creation settings
    /// </summary>
    public class NetworkCreationSettings
    {
        public Level ReferenceLevel { get; set; }
        public bool AutoSizeEnabled { get; set; } = true;
        public bool CreateStructures { get; set; } = true;
        public double MinimumSlope { get; set; } = 0.01;
    }
}