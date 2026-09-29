using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class PixelBounceAnimation
{
    public Color FromColor { get; set; }
    public Color Color { get; init; }
    public float Progress { get; set; }
}