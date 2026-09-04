using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt;

public static class ColorQuantizer
{
    private const int _defaultSampleSize = 20_000;
    private const int _defaultIterations = 8;

    public static Texture2D Quantize(GraphicsDevice graphicsDevice, Texture2D source, int colorCount, int sampleSize = _defaultSampleSize, int iterations = _defaultIterations)
    {
        if (colorCount < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(colorCount));
        }

        var width = source.Width;
        var height = source.Height;

        var pixels = new Color[width * height];
        source.GetData(pixels);

        var samples = CreateSamples(pixels, sampleSize);
        if (samples.Count == 0)
        {
            return CreateTexture(graphicsDevice, width, height, pixels);
        }

        var palette = CreateInitialPalette(samples, colorCount);

        RunKMeans(samples, palette, iterations);
        ApplyPalette(pixels, palette);

        return CreateTexture(graphicsDevice, width, height, pixels);
    }

    private static List<Vector3> CreateSamples(Color[] pixels, int sampleSize)
    {
        var samples = new List<Vector3>(Math.Min(sampleSize, pixels.Length));
        var step = Math.Max(1, pixels.Length / sampleSize);

        for (var i = 0; i < pixels.Length; i += step)
        {
            var color = pixels[i];

            if (color.A == 0)
            {
                continue;
            }

            samples.Add(ToVector(color));
        }

        return samples;
    }

    private static List<Vector3> CreateInitialPalette(List<Vector3> samples, int colorCount)
    {
        var palette = new List<Vector3>(Math.Min(colorCount, samples.Count))
        {
            samples[0]
        };

        while (palette.Count < colorCount && palette.Count < samples.Count)
        {
            var bestSample = samples[0];
            var bestDistance = -1f;

            var step = Math.Max(1, samples.Count / 5_000);

            for (var i = 0; i < samples.Count; i += step)
            {
                var sample = samples[i];

                var minDistance = float.MaxValue;

                foreach (var center in palette)
                {
                    var distance = Vector3.DistanceSquared(sample, center);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }

                if (minDistance > bestDistance)
                {
                    bestDistance = minDistance;
                    bestSample = sample;
                }
            }

            palette.Add(bestSample);
        }

        return palette;
    }

    private static void RunKMeans(List<Vector3> samples, List<Vector3> palette, int iterations)
    {
        var sums = new Vector3[palette.Count];
        var counts = new int[palette.Count];

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            Array.Clear(sums, 0, sums.Length);
            Array.Clear(counts, 0, counts.Length);

            foreach (var sample in samples)
            {
                var nearest = FindNearestColor(sample, palette);

                sums[nearest] += sample;
                counts[nearest]++;
            }

            var changed = false;

            for (var i = 0; i < palette.Count; i++)
            {
                if (counts[i] == 0)
                {
                    continue;
                }

                var newCenter = sums[i] / counts[i];

                if (Vector3.DistanceSquared(palette[i], newCenter) > 0.01f)
                {
                    changed = true;
                }

                palette[i] = newCenter;
            }

            if (!changed)
            {
                break;
            }
        }
    }

    private static void ApplyPalette(Color[] pixels, List<Vector3> palette)
    {
        for (var i = 0; i < pixels.Length; i++)
        {
            var pixel = pixels[i];

            if (pixel.A == 0)
            {
                continue;
            }

            var vector = ToVector(pixel);
            var nearest = FindNearestColor(vector, palette);
            var color = palette[nearest];

            pixels[i] = new Color(ClampToByte(color.X), ClampToByte(color.Y), ClampToByte(color.Z), pixel.A);
        }
    }

    private static int FindNearestColor(Vector3 color, List<Vector3> palette)
    {
        var nearest = 0;
        var minDistance = float.MaxValue;

        for (var i = 0; i < palette.Count; i++)
        {
            var distance = Vector3.DistanceSquared(color, palette[i]);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = i;
            }
        }

        return nearest;
    }

    private static Vector3 ToVector(Color color)
    {
        return new Vector3(color.R, color.G, color.B);
    }

    private static byte ClampToByte(float value)
    {
        return (byte)Math.Clamp((int)MathF.Round(value), 0, 255);
    }

    private static Texture2D CreateTexture(GraphicsDevice graphicsDevice, int width, int height, Color[] pixels)
    {
        var texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
        texture.SetData(pixels);

        return texture;
    }
}