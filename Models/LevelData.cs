using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Buttons;

namespace PixelArt.Models;

public class LevelData
{
    public int Id { get; set; }
    [JsonIgnore] public Texture2D Texture { get; set; }
    public List<PixelColorGroup> ColorGroups { get; set; } = [];
    public List<PixelData> Pixels { get; set; } = [];
    public List<int> History { get; set; } = [];
    
    [JsonIgnore] public Button Button { get; set; }
    public int ErrorCount { get; set; }
    [JsonIgnore] public float ErrorCountPercent => (float)ErrorCount / History.Count * 100f;
}