using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class PixelData
{
    public int Index { get; set; }
    public int TexturePositionX { get; set; }
    public int TexturePositionY { get; set; }
    public Color OriginalColor { get; set; }
    public Color GrayColor { get; set; }
    public Color CurrentColor { get; set; }
    
    [JsonIgnore] public bool IsFinished => OriginalColor == CurrentColor;

    public Vector2 GetScreenPosition(Rectangle bounds, int textureWidth, int textureHeight)
    {
        var pixelWidth = (float)bounds.Width / textureWidth;
        var pixelHeight = (float)bounds.Height / textureHeight;

        return new Vector2(
            bounds.X + TexturePositionX * pixelWidth + pixelWidth / 2f,
            bounds.Y + TexturePositionY * pixelHeight + pixelHeight / 2f
        );
    }
    
    public Vector2 GetWorldPosition(float pixelWidth, float pixelHeight)
    {
        return new Vector2(
            TexturePositionX * pixelWidth + pixelWidth / 2f,
            TexturePositionY * pixelHeight + pixelHeight / 2f
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