using System;
using System.Collections.Generic;
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
    }

    public static IEnumerable<Point> GetLine(Point start, Point end)
    {
        var x0 = start.X;
        var y0 = start.Y;

        var x1 = end.X;
        var y1 = end.Y;

        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);

        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;

        var err = dx - dy;

        while (true)
        {
            yield return new Point(x0, y0);

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var e2 = 2 * err;

            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}