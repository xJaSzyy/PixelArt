using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class PixelColorGroup
{
    public int Number { get; set; }
    public Color OriginalColor { get; set; }
    public List<PixelData> Pixels { get; set; } = [];
}