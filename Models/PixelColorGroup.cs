using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace PixelArt.Models;

public class PixelColorGroup
{
    public int Number { get; set; }
    public ColorData OriginalColor { get; set; }
    [JsonIgnore] public List<PixelData> Pixels { get; set; } = [];
    
    [JsonIgnore] public float Progress
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

    [JsonIgnore] public bool IsFinished => Progress >= 1f;
}