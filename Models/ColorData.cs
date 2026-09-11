using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class ColorData
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }

    public ColorData()
    {
    }
    
    public ColorData(Color color)
    {
        R = color.R;
        G = color.G;
        B = color.B;
    }

    public Color ToColor()
    {
        return new Color(R, G, B);
    }
}