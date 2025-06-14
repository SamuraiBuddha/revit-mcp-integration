using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Electrical;
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
        Task<List<Autodesk.Revit.DB.Plumbing.Pipe>> CreatePipesFromCenterlines(
            List<DetectedPipe> detectedPipes,
            PipeCreationSettings settings);

        /// <summary>
        /// Intelligently places fittings at pipe intersections based on angle and system type
        /// </summary>
        Task<FamilyInstance> GenerateFittingAtIntersection(
            Autodesk.Revit.DB.Plumbing.Pipe pipe1,
            Autodesk.Revit.DB.Plumbing.Pipe pipe2,
            Models.IntersectionAnalysis intersection);

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
    /// MEP creation settings
    /// </summary>
    public class MEPCreationSettings
    {
        public Level ReferenceLevel { get; set; }
        public bool AutoRoute { get; set; } = true;
        public bool AutoSize { get; set; } = true;
        public double DefaultSlope { get; set; } = 0.01; // 1% slope
        public Dictionary<RevitMcpServer.Models.MEPSystemType, string> SystemMappings { get; set; } = new Dictionary<RevitMcpServer.Models.MEPSystemType, string>();
    }

    /// <summary>
    /// Result of MEP system creation
    /// </summary>
    public class MEPCreationResult
    {
        public List<Duct> Ducts { get; set; } = new List<Duct>();
        public List<Autodesk.Revit.DB.Plumbing.Pipe> Pipes { get; set; } = new List<Autodesk.Revit.DB.Plumbing.Pipe>();
        public List<Autodesk.Revit.DB.Electrical.Conduit> Conduits { get; set; } = new List<Autodesk.Revit.DB.Electrical.Conduit>();
        public List<Autodesk.Revit.DB.Electrical.CableTray> CableTrays { get; set; } = new List<Autodesk.Revit.DB.Electrical.CableTray>();
        public List<FamilyInstance> Fittings { get; set; } = new List<FamilyInstance>();
        public List<MEPSystem> Systems { get; set; } = new List<MEPSystem>();
        
        public int TotalElementsCreated => 
            Ducts.Count + Pipes.Count + Conduits.Count + CableTrays.Count + Fittings.Count;
    }
}
