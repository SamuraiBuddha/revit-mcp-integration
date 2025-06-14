using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

namespace RevitMcpServer.ScanToBIM
{
    /// <summary>
    /// Resolves appropriate fittings for pipe connections and intersections
    /// </summary>
    public class FittingResolver
    {
        private readonly Document _doc;
        private readonly Dictionary<string, FamilySymbol> _fittingFamilyCache;

        public FittingResolver(Document doc)
        {
            _doc = doc;
            _fittingFamilyCache = new Dictionary<string, FamilySymbol>();
            LoadFittingFamilies();
        }

        /// <summary>
        /// Loads available fitting families from the document
        /// </summary>
        private void LoadFittingFamilies()
        {
            var collector = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeFitting);

            foreach (FamilySymbol symbol in collector)
            {
                var key = GetFittingKey(symbol);
                if (!_fittingFamilyCache.ContainsKey(key))
                {
                    _fittingFamilyCache[key] = symbol;
                }
            }
        }

        /// <summary>
        /// Creates a unique key for fitting identification
        /// </summary>
        private string GetFittingKey(FamilySymbol symbol)
        {
            var family = symbol.Family;
            var familyName = family.Name;
            var typeName = symbol.Name;
            
            // Extract fitting type from family name
            var fittingType = DetermineFittingTypeFromName(familyName);
            
            // Extract size info if available
            var sizeParam = symbol.LookupParameter("Nominal Diameter");
            var size = sizeParam?.AsDouble() ?? 0.0;
            
            return $"{fittingType}_{size:F3}_{familyName}_{typeName}";
        }

        /// <summary>
        /// Determines fitting type from family name
        /// </summary>
        private string DetermineFittingTypeFromName(string familyName)
        {
            var lowerName = familyName.ToLower();
            
            if (lowerName.Contains("elbow")) return "ELBOW";
            if (lowerName.Contains("tee")) return "TEE";
            if (lowerName.Contains("wye")) return "WYE";
            if (lowerName.Contains("cross")) return "CROSS";
            if (lowerName.Contains("reducer")) return "REDUCER";
            if (lowerName.Contains("coupling")) return "COUPLING";
            if (lowerName.Contains("cap")) return "CAP";
            if (lowerName.Contains("transition")) return "TRANSITION";
            
            return "GENERIC";
        }

        /// <summary>
        /// Gets the best matching fitting family for the given parameters
        /// </summary>
        public FamilySymbol GetFitting(
            string fittingType,
            double diameter1,
            double diameter2,
            string systemType)
        {
            // Try to find exact match first
            var exactKey = $"{fittingType}_{diameter1:F3}";
            if (_fittingFamilyCache.TryGetValue(exactKey, out var exactMatch))
            {
                if (!exactMatch.IsActive)
                    exactMatch.Activate();
                return exactMatch;
            }

            // Find closest size match
            var candidates = _fittingFamilyCache
                .Where(kvp => kvp.Key.StartsWith(fittingType))
                .OrderBy(kvp => 
                {
                    var parts = kvp.Key.Split('_');
                    if (parts.Length > 1 && double.TryParse(parts[1], out var size))
                    {
                        return Math.Abs(size - diameter1);
                    }
                    return double.MaxValue;
                })
                .ToList();

            if (candidates.Any())
            {
                var bestMatch = candidates.First().Value;
                if (!bestMatch.IsActive)
                    bestMatch.Activate();
                return bestMatch;
            }

            // Return first available fitting as fallback
            var fallback = _fittingFamilyCache.Values.FirstOrDefault();
            if (fallback != null && !fallback.IsActive)
                fallback.Activate();
            
            return fallback;
        }

        /// <summary>
        /// Creates appropriate fitting between two pipes
        /// </summary>
        public FamilyInstance CreateFitting(
            Pipe pipe1,
            Pipe pipe2,
            XYZ intersectionPoint,
            string fittingType)
        {
            var diameter1 = pipe1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.AsDouble() ?? 0.5;
            var diameter2 = pipe2.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.AsDouble() ?? 0.5;
            var systemType = pipe1.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM)?.AsValueString() ?? "Unknown";

            var fittingSymbol = GetFitting(fittingType, diameter1, diameter2, systemType);
            if (fittingSymbol == null)
            {
                throw new InvalidOperationException($"No suitable {fittingType} fitting found");
            }

            // Create fitting instance
            var fitting = _doc.Create.NewFamilyInstance(
                intersectionPoint,
                fittingSymbol,
                pipe1.ReferenceLevel,
                Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

            // Connect fitting to pipes
            ConnectFittingToPipes(fitting, pipe1, pipe2);

            return fitting;
        }

        /// <summary>
        /// Connects fitting to pipes using connectors
        /// </summary>
        private void ConnectFittingToPipes(FamilyInstance fitting, Pipe pipe1, Pipe pipe2)
        {
            var fittingConnectors = GetConnectors(fitting);
            var pipe1Connectors = GetConnectors(pipe1);
            var pipe2Connectors = GetConnectors(pipe2);

            // Find closest connectors
            Connector fittingConn1 = null, fittingConn2 = null;
            Connector pipeConn1 = null, pipeConn2 = null;
            double minDist1 = double.MaxValue, minDist2 = double.MaxValue;

            foreach (var fc in fittingConnectors)
            {
                foreach (var pc in pipe1Connectors)
                {
                    var dist = fc.Origin.DistanceTo(pc.Origin);
                    if (dist < minDist1)
                    {
                        minDist1 = dist;
                        fittingConn1 = fc;
                        pipeConn1 = pc;
                    }
                }

                foreach (var pc in pipe2Connectors)
                {
                    var dist = fc.Origin.DistanceTo(pc.Origin);
                    if (dist < minDist2)
                    {
                        minDist2 = dist;
                        fittingConn2 = fc;
                        pipeConn2 = pc;
                    }
                }
            }

            // Connect if connectors found
            if (fittingConn1 != null && pipeConn1 != null && !fittingConn1.IsConnected)
            {
                fittingConn1.ConnectTo(pipeConn1);
            }

            if (fittingConn2 != null && pipeConn2 != null && !fittingConn2.IsConnected)
            {
                fittingConn2.ConnectTo(pipeConn2);
            }
        }

        /// <summary>
        /// Gets connectors from an element
        /// </summary>
        private List<Connector> GetConnectors(Element element)
        {
            var connectors = new List<Connector>();

            var mepModel = element as MEPModel;
            if (mepModel != null)
            {
                var connMgr = mepModel.ConnectorManager;
                if (connMgr != null)
                {
                    foreach (Connector conn in connMgr.Connectors)
                    {
                        connectors.Add(conn);
                    }
                }
            }
            else if (element is FamilyInstance fi)
            {
                var mepModel2 = fi.MEPModel;
                if (mepModel2 != null)
                {
                    var connMgr = mepModel2.ConnectorManager;
                    if (connMgr != null)
                    {
                        foreach (Connector conn in connMgr.Connectors)
                        {
                            connectors.Add(conn);
                        }
                    }
                }
            }

            return connectors;
        }
    }
}
