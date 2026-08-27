using Microsoft.Xna.Framework;

namespace PixelArt;

public static class Colors
{
    public static readonly Color LightBackground = new(45, 45, 45);
    public static readonly Color Background = new(23, 24, 36);
    public static readonly Color DarkBackground = new(23, 23, 23);
    
    public static readonly Color Yellow = new(230, 200, 94);
    public static readonly Color Green = new(89, 194, 91);
    public static readonly Color Red = new(201, 73, 73);

    public static readonly Color Black = new(0, 0, 0);
    public static readonly Color Text = new(241, 232, 213);
    
    public static bool IsDark(Color color)
    {
        var brightness =
            0.299f * color.R +
            0.587f * color.G +
            0.114f * color.B;

        return brightness < 128;
    }
}