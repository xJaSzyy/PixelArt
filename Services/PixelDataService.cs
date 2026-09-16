using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using PixelArt.Models;

namespace PixelArt.Services;

public sealed class PixelDataService
{
    public IReadOnlyList<PixelData> Pixels => _pixels;
    public IReadOnlyList<PixelColorGroup> ColorGroups => _colorGroups;

    private PixelData[] _pixelLookup = [];
    private List<PixelData> _pixels = [];
    private List<PixelColorGroup> _colorGroups = [];

    private readonly Dictionary<Color, PixelColorGroup> _groupsByColor = [];

    public void BuildNew(Color[] pixels, int width, int height)
    {
        _pixels = [];
        _colorGroups = [];
        _groupsByColor.Clear();

        _pixelLookup = new PixelData[width * height];

        for (var i = 0; i < pixels.Length; i++)
        {
            var original = pixels[i];

            if (original.A != 255)
            {
                continue;
            }

            if (!_groupsByColor.TryGetValue(original, out var group))
            {
                group = new PixelColorGroup
                {
                    OriginalColor = new ColorData(original),
                    Pixels = []
                };

                _groupsByColor.Add(original, group);
                _colorGroups.Add(group);
            }

            var pixel = new PixelData
            {
                Index = i,
                OriginalColor = new ColorData(original),
                CurrentColor = new ColorData(Color.White)
            };

            _pixels.Add(pixel);
            _pixelLookup[i] = pixel;
            group.Pixels.Add(pixel);
        }

        SortAndNumberColorGroups();
    }

    public void Rebuild(LevelData level)
    {
        _pixels = level.Pixels.ToList();
        _colorGroups = [];
        _groupsByColor.Clear();

        _pixelLookup = new PixelData[
            level.Texture.Width * level.Texture.Height];

        foreach (var pixel in _pixels)
        {
            _pixelLookup[pixel.Index] = pixel;

            var color = pixel.OriginalColor.ToColor();

            if (!_groupsByColor.TryGetValue(color, out var group))
            {
                group = new PixelColorGroup
                {
                    OriginalColor = pixel.OriginalColor,
                    Pixels = []
                };

                _groupsByColor.Add(color, group);
                _colorGroups.Add(group);
            }

            group.Pixels.Add(pixel);
        }

        SortAndNumberColorGroups();
    }

    public PixelData GetPixel(int index)
    {
        if (index < 0 || index >= _pixelLookup.Length)
        {
            return null;
        }

        return _pixelLookup[index];
    }

    public bool TryGetGroup(Color color, out PixelColorGroup group)
    {
        return _groupsByColor.TryGetValue(color, out group);
    }

    private void SortAndNumberColorGroups()
    {
        _colorGroups = _colorGroups
            .OrderByDescending(x =>
                0.2126 * x.OriginalColor.R +
                0.7152 * x.OriginalColor.G +
                0.0722 * x.OriginalColor.B)
            .ToList();

        for (var i = 0; i < _colorGroups.Count; i++)
        {
            _colorGroups[i].Number = _colorGroups.Count - i;
        }
    }
}