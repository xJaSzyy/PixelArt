using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Buttons;

namespace PixelArt.Models;

public class LevelData
{
    public Texture2D Texture { get; set; }
    public Dictionary<Color, PixelColorGroup> ColorGroups { get; set; } = new();
    public Dictionary<int, PixelData> Pixels { get; set; } = [];
    public Color[] TexturePixels { get; set; }
    
    public bool IsLoaded { get; set; }
    public Button Button { get; set; }
}