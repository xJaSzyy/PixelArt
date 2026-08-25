using Microsoft.Xna.Framework;

namespace PixelArt;

public static class Colors
{
    public static Color Yellow = new(230, 200, 94);
    public static Color Green = new(89, 194, 91);
    public static Color Red = new(201, 73, 73);
    
    public static bool IsDark(Color color)
    {
        var brightness =
            0.299f * color.R +
            0.587f * color.G +
            0.114f * color.B;

        return brightness < 128;
    }
}