using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public struct PixelData
{
    public int Index;
    public Color OriginalColor;
    public Color GrayColor;

    public PixelData(int index, Color originalColor, Color grayColor)
    {
        Index = index;
        OriginalColor = originalColor;
        GrayColor = grayColor;
    }
}