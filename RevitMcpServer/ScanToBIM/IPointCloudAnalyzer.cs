using System.Collections.Generic;
using System.Threading.Tasks;
using RevitMcpServer.Models;

namespace RevitMcpServer.ScanToBIM
{
    public interface IPointCloudAnalyzer
    {
        Task<List<DetectedPipe>> DetectPipes(string regionId, double confidenceThreshold);
        Task<List<DetectedMEPElement>> ClassifyMEPElements(string regionId);
        Task<double> AnalyzeConfidence(DetectedPipe pipe);
    }
}
