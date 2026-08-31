using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class Pixel
{
    public int X { get; set; }
    public int Y { get; set; }
    public Color Color { get; set; }
    public Point Position => new(X, Y);
}