using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class PixelColorGroup
{
    public int Number { get; set; }
    public Color OriginalColor { get; set; }
    public Color GrayColor { get; set; }
    public List<PixelData> Pixels { get; set; } = [];
    
    public bool GrayColorIsDark()
    {
        var brightness =
            0.299f * GrayColor.R +
            0.587f * GrayColor.G +
            0.114f * GrayColor.B;

        return brightness < 128;
    }
}