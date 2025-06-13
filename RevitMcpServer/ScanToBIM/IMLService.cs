using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using RevitMcpServer.Models;

namespace RevitMcpServer.ScanToBIM
{
    /// <summary>
    /// Interface for machine learning service integration
    /// </summary>
    public interface IMLService
    {
        /// <summary>
        /// Detects cylindrical objects in point cloud data
        /// </summary>
        Task<List<CylindricalObject>> DetectCylinders(List<XYZ> points);

        /// <summary>
        /// Classifies MEP objects
        /// </summary>
        Task<MEPType> ClassifyMEPObject(DetectedObject obj);

        /// <summary>
        /// Detects planes in point cloud
        /// </summary>
        Task<List<Plane>> DetectPlanes(PointCloudInstance pointCloud);
    }

    /// <summary>
    /// Represents a cylindrical object detected in point cloud
    /// </summary>
    public class CylindricalObject
    {
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public double Radius { get; set; }
        public double Length { get; set; }
        public double Confidence { get; set; }
        public Color AverageColor { get; set; }
        public double Reflectivity { get; set; }
    }

    /// <summary>
    /// MEP element types
    /// </summary>
    public enum MEPType
    {
        HVACDuct,
        Piping,
        Conduit,
        CableTray,
        Unknown
    }
}