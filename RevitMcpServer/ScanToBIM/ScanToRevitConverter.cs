using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;
using RevitMcpServer.Models;

namespace RevitMcpServer.ScanToBIM
{
    /// <summary>
    /// Converts detected scan objects into Revit elements with intelligent fitting placement
    /// </summary>
    public class ScanToRevitConverter : IScanToRevitConverter
    {
        private readonly Document _doc;
        private readonly FittingResolver _fittingResolver;
        
        public ScanToRevitConverter(Document doc)
        {
            _doc = doc;
            _fittingResolver = new FittingResolver(doc);
        }

        /// <summary>
        /// Creates Revit pipes from detected centerlines with proper system classification
        /// </summary>
        public async Task<List<Pipe>> CreatePipesFromCenterlines(
            List<DetectedPipe> detectedPipes,
            PipeCreationSettings settings)
        {
            var createdPipes = new List<Pipe>();
            
            using (var trans = new Transaction(_doc, "Create Pipes from Scan"))
            {
                trans.Start();
                
                // Group pipes by system type for batch processing
                var pipesBySystem = detectedPipes.GroupBy(p => p.SystemType);
                
                foreach (var systemGroup in pipesBySystem)
                {
                    var systemType = GetOrCreateSystemType(systemGroup.Key, settings);
                    
                    foreach (var detectedPipe in systemGroup)
                    {
                        try
                        {
                            var pipe = CreatePipeFromDetection(detectedPipe, systemType, settings);
                            createdPipes.Add(pipe);
                            
                            // Set additional parameters from scan data
                            SetPipeParametersFromScan(pipe, detectedPipe);
                        }
                        catch (Exception ex)
                        {
                            // Log error but continue processing
                            LogError($"Failed to create pipe: {ex.Message}", detectedPipe);
                        }
                    }
                }
                
                // Connect pipes and add fittings
                await ConnectPipesWithFittings(createdPipes);
                
                trans.Commit();
            }
            
            return createdPipes;
        }

        /// <summary>
        /// Intelligently places fittings at pipe intersections based on angle and system type
        /// </summary>
        public async Task<FamilyInstance> GenerateFittingAtIntersection(
            Pipe pipe1, 
            Pipe pipe2,
            IntersectionAnalysis intersection)
        {
            // Determine fitting type based on intersection geometry
            var fittingType = DetermineFittingType(intersection);
            
            // Get appropriate fitting family
            var fittingFamily = await GetFittingFamily(
                fittingType,
                pipe1.Diameter,
                pipe2.Diameter,
                pipe1.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM)?.AsValueString()
            );
            
            if (fittingFamily == null)
            {
                throw new InvalidOperationException($"No suitable {fittingType} fitting found");
            }
            
            using (var trans = new Transaction(_doc, "Place Fitting"))
            {
                trans.Start();
                
                FamilyInstance fitting = null;
                
                switch (fittingType)
                {
                    case FittingType.Elbow:
                        fitting = CreateElbow(pipe1, pipe2, intersection);
                        break;
                        
                    case FittingType.Tee:
                        fitting = CreateTee(pipe1, pipe2, intersection);
                        break;
                        
                    case FittingType.Wye:
                        fitting = CreateWye(pipe1, pipe2, intersection);
                        break;
                        
                    case FittingType.Cross:
                        fitting = CreateCross(intersection.AllPipes);
                        break;
                        
                    case FittingType.Reducer:
                        fitting = CreateReducer(pipe1, pipe2);
                        break;
                }
                
                trans.Commit();
                return fitting;
            }
        }

        /// <summary>
        /// Creates complete underground utility networks with proper materials and fittings
        /// </summary>
        public async Task<UtilityNetworkResult> BatchCreateUndergroundUtilities(
            List<UndergroundUtilityData> utilityData,
            BatchCreationSettings settings)
        {
            var result = new UtilityNetworkResult();
            
            using (var transGroup = new TransactionGroup(_doc, "Create Underground Network"))
            {
                transGroup.Start();
                
                // Create pipes by material type for efficiency
                var pipesByMaterial = utilityData.GroupBy(u => u.Material);
                
                foreach (var materialGroup in pipesByMaterial)
                {
                    using (var trans = new Transaction(_doc, $"Create {materialGroup.Key} Pipes"))
                    {
                        trans.Start();
                        
                        var pipeType = GetPipeTypeForMaterial(materialGroup.Key, settings);
                        
                        foreach (var utility in materialGroup)
                        {
                            try
                            {
                                var pipes = await CreateUndergroundPipeRun(utility, pipeType, settings);
                                result.CreatedPipes.AddRange(pipes);
                                
                                // Add structures at key points
                                var structures = await PlaceStructures(utility, pipes, settings);
                                result.CreatedStructures.AddRange(structures);
                            }
                            catch (Exception ex)
                            {
                                result.Errors.Add(new CreationError
                                {
                                    UtilityData = utility,
                                    ErrorMessage = ex.Message
                                });
                            }
                        }
                        
                        trans.Commit();
                    }
                }
                
                // Post-process to ensure connectivity
                EnsureNetworkConnectivity(result);
                
                transGroup.Assimilate();
            }
            
            return result;
        }

        /// <summary>
        /// Creates MEP systems from classified scan data
        /// </summary>
        public async Task<MEPCreationResult> CreateMEPSystemsFromScan(
            MEPClassification classification,
            MEPCreationSettings settings)
        {
            var result = new MEPCreationResult();
            
            using (var transGroup = new TransactionGroup(_doc, "Create MEP Systems from Scan"))
            {
                transGroup.Start();
                
                // Create HVAC ducts
                if (classification.HVACDucts.Any())
                {
                    result.Ducts = await CreateDuctsFromDetection(classification.HVACDucts, settings);
                }
                
                // Create piping
                if (classification.Pipes.Any())
                {
                    result.Pipes = await CreatePipesFromDetection(classification.Pipes, settings);
                }
                
                // Create conduits
                if (classification.Conduits.Any())
                {
                    result.Conduits = await CreateConduitsFromDetection(classification.Conduits, settings);
                }
                
                // Create cable trays
                if (classification.CableTrays.Any())
                {
                    result.CableTrays = await CreateCableTraysFromDetection(classification.CableTrays, settings);
                }
                
                // Connect systems
                await ConnectMEPSystems(result);
                
                transGroup.Assimilate();
            }
            
            return result;
        }

        #region Helper Methods

        private Pipe CreatePipeFromDetection(
            DetectedPipe detected,
            PipingSystemType systemType,
            PipeCreationSettings settings)
        {
            // Get appropriate pipe type based on size
            var pipeType = SelectPipeType(detected.Diameter, systemType, settings);
            
            // Create pipe
            var pipe = Pipe.Create(
                _doc,
                systemType.Id,
                pipeType.Id,
                settings.ReferenceLevel.Id,
                detected.Centerline.GetEndPoint(0),
                detected.Centerline.GetEndPoint(1)
            );
            
            // Set diameter
            var diamParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            if (diamParam != null && !diamParam.IsReadOnly)
            {
                diamParam.Set(detected.Diameter);
            }
            
            return pipe;
        }

        private FittingType DetermineFittingType(IntersectionAnalysis intersection)
        {
            var angle = intersection.Angle;
            var pipeCount = intersection.AllPipes.Count;
            
            if (pipeCount == 2)
            {
                if (Math.Abs(angle - 90) < 5) return FittingType.Elbow;
                if (angle < 45) return FittingType.Reducer; // Likely size transition
                return FittingType.Elbow; // Non-90 degree elbow
            }
            else if (pipeCount == 3)
            {
                // Check if it's a wye or tee based on angles
                if (intersection.IsGravitySystem && angle < 60)
                    return FittingType.Wye;
                return FittingType.Tee;
            }
            else if (pipeCount == 4)
            {
                return FittingType.Cross;
            }
            
            throw new InvalidOperationException($"Cannot determine fitting for {pipeCount} pipes");
        }

        private async Task<List<Pipe>> CreateUndergroundPipeRun(
            UndergroundUtilityData utilityData,
            PipeType pipeType,
            BatchCreationSettings settings)
        {
            var pipes = new List<Pipe>();
            
            foreach (var segment in utilityData.Segments)
            {
                // Adjust for proper burial depth
                var adjustedStart = new XYZ(
                    segment.Start.X,
                    segment.Start.Y,
                    segment.Start.Z - utilityData.BurialDepth
                );
                
                var adjustedEnd = new XYZ(
                    segment.End.X,
                    segment.End.Y,
                    segment.End.Z - utilityData.BurialDepth
                );
                
                var pipe = Pipe.Create(
                    _doc,
                    pipeType.Id,
                    settings.UndergroundLevel.Id,
                    null,
                    Line.CreateBound(adjustedStart, adjustedEnd)
                );
                
                // Set underground-specific parameters
                SetUndergroundParameters(pipe, utilityData);
                
                pipes.Add(pipe);
            }
            
            return pipes;
        }

        private void SetUndergroundParameters(Pipe pipe, UndergroundUtilityData data)
        {
            // Set material
            var materialParam = pipe.LookupParameter("Pipe Material");
            materialParam?.Set(data.Material.ToString());
            
            // Set class/schedule
            var classParam = pipe.LookupParameter("Pipe Class");
            classParam?.Set(data.PipeClass);
            
            // Set burial depth
            var depthParam = pipe.LookupParameter("Burial Depth");
            depthParam?.Set(data.BurialDepth);
            
            // Set installation year if known
            if (data.InstallationYear.HasValue)
            {
                var yearParam = pipe.LookupParameter("Installation Year");
                yearParam?.Set(data.InstallationYear.Value);
            }
        }

        private async Task<List<FamilyInstance>> PlaceStructures(
            UndergroundUtilityData utilityData,
            List<Pipe> pipes,
            BatchCreationSettings settings)
        {
            var structures = new List<FamilyInstance>();
            
            // Place manholes at direction changes and intervals
            foreach (var location in utilityData.StructureLocations)
            {
                var structureType = DetermineStructureType(location, utilityData);
                var family = await GetStructureFamily(structureType, settings);
                
                if (family != null)
                {
                    var instance = _doc.Create.NewFamilyInstance(
                        location.Point,
                        family,
                        settings.UndergroundLevel,
                        Autodesk.Revit.DB.Structure.StructuralType.NonStructural
                    );
                    
                    // Set rim elevation
                    var rimParam = instance.LookupParameter("Rim Elevation");
                    rimParam?.Set(location.RimElevation);
                    
                    structures.Add(instance);
                }
            }
            
            return structures;
        }

        #endregion
    }

    #region Supporting Enums

    public enum FittingType
    {
        Elbow,
        Tee,
        Wye,
        Cross,
        Reducer,
        Coupling,
        Cap,
        Transition
    }

    #endregion
}
