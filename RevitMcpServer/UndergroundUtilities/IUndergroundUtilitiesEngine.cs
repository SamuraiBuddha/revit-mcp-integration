using Autodesk.Revit.DB;
using RevitMcpServer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RevitMcpServer.UndergroundUtilities
{
    public interface IUndergroundUtilitiesEngine
    {
        DepthAnalysisResult AnalyzeBurialDepths(List<Element> utilities, Autodesk.Revit.DB.Architecture.TopographySurface surface);
        List<UtilityCorridor> GenerateUtilityCorridors(List<Element> utilities, CorridorSettings settings);
        List<InvertData> ExtractInvertElevations(List<Pipe> pipes);
        List<UndergroundClash> DetectUndergroundClashes(List<Element> existing, List<Element> proposed, ClashDetectionSettings settings);
        Task<UndergroundNetwork> CreateUndergroundNetwork(List<DetectedPipe> detectedPipes, NetworkCreationSettings settings);
        Task<GPRIntegrationResult> IntegrateGPRData(object gprData, object pointCloud, object transform);
    }
}
