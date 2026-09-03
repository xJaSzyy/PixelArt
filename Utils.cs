using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt;

public static class Utils
{
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        var t = MathHelper.Clamp(
            (value - fromMin) / (fromMax - fromMin),
            0f,
            1f);

        return MathHelper.Lerp(toMin, toMax, t);
    }
    
    public static Color GenerateGrayColor(int index, int total)
    {
        if (total <= 1)
        {
            return new Color(220, 220, 220);
        }
        
        const int min = 150;
        const int max = 230;
        
        var value = max - index * (max - min) / (total - 1);
        
        return new Color(value, value, value);
    }

    public static Texture2D CloneTexture2D(GraphicsDevice graphicsDevice, Texture2D texture)
    {
        var clone = new Texture2D(
            graphicsDevice,
            texture.Width,
            texture.Height,
            texture.LevelCount > 1,
            texture.Format
        );

        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);
        clone.SetData(data);

        return clone;
    }

    public static float DistanceToLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        var line = lineEnd - lineStart;

        if (line.LengthSquared() == 0)
            return Vector2.Distance(point, lineStart);

        var cross = MathF.Abs(
            line.X * (lineStart.Y - point.Y) -
            (lineStart.X - point.X) * line.Y);

        return cross / line.Length();
    }}