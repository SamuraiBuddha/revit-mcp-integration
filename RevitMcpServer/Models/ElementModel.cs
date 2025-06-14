using System.Collections.Generic;
using Newtonsoft.Json;

namespace RevitMcpServer.Models
{
    /// <summary>
    /// Model for Revit elements with essential properties and parameters
    /// </summary>
    public class ElementModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("category")]
        public string Category { get; set; }
        
        [JsonProperty("parameters")]
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        
        [JsonProperty("location")]
        public LocationInfo Location { get; set; }
        
        [JsonProperty("boundingBox")]
        public BoundingBoxInfo BoundingBox { get; set; }
    }
    
    /// <summary>
    /// Location information for Revit elements
    /// </summary>
    public class LocationInfo
    {
        [JsonProperty("x")]
        public double X { get; set; }
        
        [JsonProperty("y")]
        public double Y { get; set; }
        
        [JsonProperty("z")]
        public double Z { get; set; }
        
        [JsonProperty("rotation")]
        public double Rotation { get; set; }
    }
    
    /// <summary>
    /// Bounding box information for Revit elements
    /// </summary>
    public class BoundingBoxInfo
    {
        [JsonProperty("min")]
        public Point3D Min { get; set; }
        
        [JsonProperty("max")]
        public Point3D Max { get; set; }
    }
    
    /// <summary>
    /// 3D point representation
    /// </summary>
    public class Point3D
    {
        [JsonProperty("x")]
        public double X { get; set; }
        
        [JsonProperty("y")]
        public double Y { get; set; }
        
        [JsonProperty("z")]
        public double Z { get; set; }
    }
}
