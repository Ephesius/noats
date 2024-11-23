using System.Text.Json.Serialization;

namespace Noats.Models;

public class NoatState
{
    public string Content { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsVisible { get; set; }
    public string ThemeName { get; set; } = "LemonDrop";
    public DateTime LastModified { get; set; }
}