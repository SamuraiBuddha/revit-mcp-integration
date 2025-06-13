using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RevitMcpServer.Models
{
    /// <summary>
    /// Model for Revit elements with essential properties and parameters
    /// </summary>
    public class ElementModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("category")]
        public string Category { get; set; }
        
        [JsonPropertyName("parameters")]
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        
        [JsonPropertyName("location")]
        public LocationInfo Location { get; set; }
        
        [JsonPropertyName("boundingBox")]
        public BoundingBoxInfo BoundingBox { get; set; }
    }
    
    /// <summary>
    /// Location information for Revit elements
    /// </summary>
    public class LocationInfo
    {
        [JsonPropertyName("x")]
        public double X { get; set; }
        
        [JsonPropertyName("y")]
        public double Y { get; set; }
        
        [JsonPropertyName("z")]
        public double Z { get; set; }
        
        [JsonPropertyName("rotation")]
        public double Rotation { get; set; }
    }
    
    /// <summary>
    /// Bounding box information for Revit elements
    /// </summary>
    public class BoundingBoxInfo
    {
        [JsonPropertyName("min")]
        public Point3D Min { get; set; }
        
        [JsonPropertyName("max")]
        public Point3D Max { get; set; }
    }
    
    /// <summary>
    /// 3D point representation
    /// </summary>
    public class Point3D
    {
        [JsonPropertyName("x")]
        public double X { get; set; }
        
        [JsonPropertyName("y")]
        public double Y { get; set; }
        
        [JsonPropertyName("z")]
        public double Z { get; set; }
    }
}