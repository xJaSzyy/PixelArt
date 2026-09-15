using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class PixelData
{
    public int Index { get; set; }

    public ColorData OriginalColor { get; set; }
    public ColorData GrayColor { get; set; }
    public ColorData CurrentColor { get; set; }

    [JsonIgnore] public bool IsFinished =>
        OriginalColor.R == CurrentColor.R 
        && OriginalColor.G == CurrentColor.G 
        && OriginalColor.B == CurrentColor.B;

    private int GetX(int textureWidth) => Index % textureWidth;
    private int GetY(int textureWidth) => Index / textureWidth;

    public Vector2 GetScreenPosition(Rectangle bounds, int textureWidth, int textureHeight)
    {
        var pixelWidth = (float)bounds.Width / textureWidth;
        var pixelHeight = (float)bounds.Height / textureHeight;

        var x = GetX(textureWidth);
        var y = GetY(textureWidth);

        return new Vector2(
            bounds.X + x * pixelWidth + pixelWidth / 2f,
            bounds.Y + y * pixelHeight + pixelHeight / 2f
        );
    }

    public Vector2 GetWorldPosition(float pixelWidth, float pixelHeight, int textureWidth)
    {
        var x = GetX(textureWidth);
        var y = GetY(textureWidth);

        return new Vector2(
            x * pixelWidth + pixelWidth / 2f,
            y * pixelHeight + pixelHeight / 2f
        );
    }
}