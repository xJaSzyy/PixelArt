using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class PixelData
{
    public Point TexturePosition { get; set; }
    public Color OriginalColor { get; set; }
    public Color CurrentColor { get; set; }
    public bool IsFinished => OriginalColor == CurrentColor;

    public Vector2 GetScreenPosition(
        Rectangle bounds,
        int textureWidth,
        int textureHeight)
    {
        var pixelWidth = (float)bounds.Width / textureWidth;
        var pixelHeight = (float)bounds.Height / textureHeight;

        return new Vector2(
            bounds.X + TexturePosition.X * pixelWidth + pixelWidth / 2f,
            bounds.Y + TexturePosition.Y * pixelHeight + pixelHeight / 2f
        );
    }
    
    public bool ColorIsDark()
    {
        var brightness =
            0.299f * CurrentColor.R +
            0.587f * CurrentColor.G +
            0.114f * CurrentColor.B;

        return brightness < 128;
    }
}