using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class Pixel
{
    public int X { get; set; }
    public int Y { get; set; }
    public Color CurrentColor { get; set; }
    public Color OriginalColor { get; set; }
    public Color GrayColor { get; set; }
    public Point Position => new(X, Y);

    public bool IsFinished => CurrentColor == OriginalColor;
}