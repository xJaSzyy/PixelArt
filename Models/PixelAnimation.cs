using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class PixelAnimation
{
    public Color FromColor { get; set; }
    public Color ToColor { get; init; }
    public float Progress { get; set; }
}