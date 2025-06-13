using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using RevitMcpServer.Models;

namespace RevitMcpServer.ScanToBIM
{
    /// <summary>
    /// Interface for scan to Revit conversion operations
    /// </summary>
    public interface IScanToRevitConverter
    {
        /// <summary>
        /// Creates Revit pipes from detected centerlines with proper system classification
        /// </summary>
        Task<List<Pipe>> CreatePipesFromCenterlines(
            List<DetectedPipe> detectedPipes,
            PipeCreationSettings settings);

        /// <summary>
        /// Intelligently places fittings at pipe intersections based on angle and system type
        /// </summary>
        Task<FamilyInstance> GenerateFittingAtIntersection(
            Pipe pipe1,
            Pipe pipe2,
            IntersectionAnalysis intersection);

        /// <summary>
        /// Creates complete underground utility networks with proper materials and fittings
        /// </summary>
        Task<UtilityNetworkResult> BatchCreateUndergroundUtilities(
            List<UndergroundUtilityData> utilityData,
            BatchCreationSettings settings);

        /// <summary>
        /// Creates MEP systems from classified scan data
        /// </summary>
        Task<MEPCreationResult> CreateMEPSystemsFromScan(
            MEPClassification classification,
            MEPCreationSettings settings);
    }

    /// <summary>
    /// MEP classification from scan analysis
    /// </summary>
    public class MEPClassification
    {
        public List<DetectedMEPElement> HVACDucts { get; set; } = new List<DetectedMEPElement>();
        public List<DetectedMEPElement> Pipes { get; set; } = new List<DetectedMEPElement>();
        public List<DetectedMEPElement> Conduits { get; set; } = new List<DetectedMEPElement>();
        public List<DetectedMEPElement> CableTrays { get; set; } = new List<DetectedMEPElement>();
        public double OverallConfidence { get; set; }
    }

    /// <summary>
    /// MEP creation settings
    /// </summary>
    public class MEPCreationSettings
    {
        public Level ReferenceLevel { get; set; }
        public bool AutoRoute { get; set; } = true;
        public bool AutoSize { get; set; } = true;
        public double DefaultSlope { get; set; } = 0.01; // 1% slope
        public Dictionary<MEPSystemType, string> SystemMappings { get; set; } = new Dictionary<MEPSystemType, string>();
    }

    /// <summary>
    /// Result of MEP system creation
    /// </summary>
    public class MEPCreationResult
    {
        public List<Duct> Ducts { get; set; } = new List<Duct>();
        public List<Pipe> Pipes { get; set; } = new List<Pipe>();
        public List<Conduit> Conduits { get; set; } = new List<Conduit>();
        public List<CableTray> CableTrays { get; set; } = new List<CableTray>();
        public List<FamilyInstance> Fittings { get; set; } = new List<FamilyInstance>();
        public List<MEPSystem> Systems { get; set; } = new List<MEPSystem>();
        
        public int TotalElementsCreated => 
            Ducts.Count + Pipes.Count + Conduits.Count + CableTrays.Count + Fittings.Count;
    }

    /// <summary>
    /// Utility network creation result
    /// </summary>
    public class UtilityNetworkResult
    {
        public List<Pipe> CreatedPipes { get; set; } = new List<Pipe>();
        public List<FamilyInstance> CreatedStructures { get; set; } = new List<FamilyInstance>();
        public List<FamilyInstance> CreatedFittings { get; set; } = new List<FamilyInstance>();
        public List<CreationError> Errors { get; set; } = new List<CreationError>();
        
        public bool HasErrors => Errors.Count > 0;
        public int TotalElementsCreated => 
            CreatedPipes.Count + CreatedStructures.Count + CreatedFittings.Count;
    }

    /// <summary>
    /// Creation error information
    /// </summary>
    public class CreationError
    {
        public string ErrorMessage { get; set; }
        public string ElementType { get; set; }
        public UndergroundUtilityData UtilityData { get; set; }
        public Exception Exception { get; set; }
    }
}
