using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using PixelArt.Models;

namespace PixelArt.Services;

public sealed class PixelHighlightService
{
    private readonly HashSet<int> _highlightedPixels = [];
    private readonly Color _highlightColor = new(72, 72, 72);

    public void Highlight(PixelColorGroup group, PixelDataService pixelDataService, Action<PixelData> onPixelChanged)
    {
        Clear(pixelDataService, onPixelChanged);

        foreach (var pixel in group.Pixels.Where(pixel => !pixel.IsFinished))
        {
            pixel.CurrentColor = new ColorData(_highlightColor);

            _highlightedPixels.Add(pixel.Index);

            onPixelChanged(pixel);
        }
    }

    public void Clear(PixelDataService pixelDataService, Action<PixelData> onPixelChanged)
    {
        foreach (var index in _highlightedPixels)
        {
            var pixel = pixelDataService.GetPixel(index);

            if (pixel == null || pixel.IsFinished)
            {
                continue;
            }

            pixel.CurrentColor = pixel.GrayColor;

            onPixelChanged(pixel);
        }

        _highlightedPixels.Clear();
    }
    
    public void Reset()
    {
        _highlightedPixels.Clear();
    }

    public void Remove(int index)
    {
        _highlightedPixels.Remove(index);
    }

    public bool IsHighlighted(int index)
    {
        return _highlightedPixels.Contains(index);
    }

    public bool Contains(int index)
    {
        return _highlightedPixels.Contains(index);
    }

    public bool HasPixelOnScreen(Rectangle bounds, Vector2 pixelSize, float zoom, int textureWidth, int graphicsWidth, int graphicsHeight)
    {
        if (_highlightedPixels.Count == 0)
        {
            return false;
        }

        var pixelWidth = pixelSize.X * zoom;
        var pixelHeight = pixelSize.Y * zoom;

        foreach (var index in _highlightedPixels)
        {
            var x = index % textureWidth;
            var y = index / textureWidth;

            var pixelLeft = bounds.X + x * pixelWidth;
            var pixelTop = bounds.Y + y * pixelHeight;
            var pixelRight = pixelLeft + pixelWidth;
            var pixelBottom = pixelTop + pixelHeight;

            if (pixelRight >= 0 &&
                pixelLeft < graphicsWidth &&
                pixelBottom >= 0 &&
                pixelTop < graphicsHeight)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetScreenPosition(int index, PixelDataService pixelDataService, Rectangle bounds, Vector2 pixelSize, float zoom, int textureWidth, out Vector2 position)
    {
        position = default;

        var pixel = pixelDataService.GetPixel(index);

        if (pixel == null || pixel.IsFinished)
        {
            return false;
        }

        var pixelWidth = pixelSize.X * zoom;
        var pixelHeight = pixelSize.Y * zoom;

        var x = index % textureWidth;
        var y = index / textureWidth;

        position = new Vector2(
            bounds.X + (x + 0.5f) * pixelWidth,
            bounds.Y + (y + 0.5f) * pixelHeight);

        return true;
    }

    public bool TryGetNearestScreenPosition(Vector2 fromPosition, PixelDataService pixelDataService, Rectangle bounds, Vector2 pixelSize, float zoom, int textureWidth, out int index, out Vector2 position)
    {
        index = -1;
        position = default;

        if (_highlightedPixels.Count == 0)
        {
            return false;
        }

        var minDistanceSquared = float.MaxValue;

        foreach (var pixelIndex in _highlightedPixels)
        {
            if (!TryGetScreenPosition(pixelIndex, pixelDataService, bounds, pixelSize, zoom, textureWidth, out var pixelPosition))
            {
                continue;
            }

            var distanceSquared = Vector2.DistanceSquared(fromPosition, pixelPosition);

            if (distanceSquared < minDistanceSquared)
            {
                minDistanceSquared = distanceSquared;
                index = pixelIndex;
                position = pixelPosition;
            }
        }

        return index >= 0;
    }
}