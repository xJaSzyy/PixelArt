using Microsoft.Xna.Framework;

namespace PixelArt;

public static class Colors
{
    public static Color LightBackground = new Color(45, 45, 45);
    public static Color Background = new Color(30, 30, 30);
    public static Color DarkBackground = new Color(23, 23, 23);
    
    public static Color Yellow = new(230, 200, 94);
    public static Color Green = new(89, 194, 91);
    public static Color Red = new(201, 73, 73);

    public static Color Black = new(0, 0, 0);
    
    public static bool IsDark(Color color)
    {
        var brightness =
            0.299f * color.R +
            0.587f * color.G +
            0.114f * color.B;

        return brightness < 128;
    }
}