using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Buttons;

namespace PixelArt.Models;

public class LevelData
{
    public int Id { get; set; }
    [JsonIgnore] public Texture2D Texture { get; set; }
    [JsonIgnore] public Texture2D OriginalTexture { get; set; }
    public List<PixelColorGroup> ColorGroups { get; set; } = [];
    public List<PixelData> Pixels { get; set; } = [];
    public List<int> History { get; set; } = [];
    public bool IsFinished { get; set; } = false;
    public bool IsLocked { get; set; } = false;
    
    [JsonIgnore] public Button Button { get; set; }
}