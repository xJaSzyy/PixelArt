using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class PixelColorGroup
{
    public int Number { get; set; }
    public Color OriginalColor { get; set; }
    public List<PixelData> Pixels { get; set; } = [];
    
    public float Progress
    {
        get
        {
            if (Pixels.Count == 0)
            {
                return 0;
            }

            return Pixels.Count(x => x.IsFinished) / (float)Pixels.Count;
        }
    }

    public bool IsFinished => Progress >= 1f;
}