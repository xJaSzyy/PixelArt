using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PixelArt;

public static class Utils
{
    public static void Shuffle<T>(this IList<T> list)
    {
        var random = Random.Shared;

        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    
    public static float Remap(
        float value,
        float fromMin,
        float fromMax,
        float toMin,
        float toMax)
    {
        var t = MathHelper.Clamp(
            (value - fromMin) / (fromMax - fromMin),
            0f,
            1f);

        return MathHelper.Lerp(toMin, toMax, t);
    }
}