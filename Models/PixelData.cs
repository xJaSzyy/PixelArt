using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class PixelData
{
    public int Index { get; set; }
    public Point TexturePosition { get; set; }

    public Vector2 GetScreenPosition(
        Rectangle bounds,
        int textureWidth,
        int textureHeight)
    {
        float pixelWidth = (float)bounds.Width / textureWidth;
        float pixelHeight = (float)bounds.Height / textureHeight;

        return new Vector2(
            bounds.X + TexturePosition.X * pixelWidth + pixelWidth / 2f,
            bounds.Y + TexturePosition.Y * pixelHeight + pixelHeight / 2f
        );
    }
}