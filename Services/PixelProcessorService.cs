using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Models;

namespace PixelArt.Services;

public class PixelProcessorService
{
    private Texture2D _imageTexture;
    private readonly Dictionary<Color, PixelColorGroup> _colorGroups = new();
    private readonly Dictionary<int, PixelData> _pixels = [];
    private Color[] _texturePixels;
    
    public void SetTexture(Texture2D texture)
    {
        _imageTexture = texture;
    }

    public void Generate()
    {
        var width = _imageTexture.Width;

        _texturePixels = new Color[width * _imageTexture.Height];
        _imageTexture.GetData(_texturePixels);

        _colorGroups.Clear();
        _pixels.Clear();
        
        for (var i = 0; i < _texturePixels.Length; i++)
        {
            var original = _texturePixels[i];

            if (original.A == 0)
            {
                continue;
            }
            
            if (!_colorGroups.ContainsKey(original))
            {
                _colorGroups[original] = new PixelColorGroup
                {
                    Number = 0,
                    OriginalColor = original,
                    Pixels = []
                };
            }
            
            var point = new Point(
                i % width,
                i / width
            );

            var pixel = new PixelData
            {
                Index = i,
                TexturePosition = point,
                OriginalColor = original,
                CurrentColor = Color.White
            };

            _pixels[i] = pixel;
            _colorGroups[original].Pixels.Add(pixel);
        }
        
        var sortedGroups = _colorGroups.Values
            .OrderByDescending(x => x.Pixels.Count)
            .ToList();
        
        var total = sortedGroups.Count;

        for (var i = 0; i < total; i++)
        {
            var gray = GenerateGrayValue(i, total);

            var previewColor = new Color(gray,
                gray,
                gray);

            var group = sortedGroups[i];

            group.Number = i + 1;

            foreach (var pixel in group.Pixels)
            {
                pixel.CurrentColor = previewColor;
                pixel.GrayColor = previewColor;

                var index =
                    pixel.TexturePosition.Y * width +
                    pixel.TexturePosition.X;

                _texturePixels[index] = previewColor;
            }
        }

        _imageTexture.SetData(_texturePixels);
    }

    private byte GenerateGrayValue(int index, int total)
    {
        if (total <= 1)
        {
            return 220;
        }
        
        var min = 150;
        var max = 230;
        
        var value = max -
                    index * (max - min) /
                    (total - 1);
        
        return (byte)value;
    }

    public void SetPixel(int index, Color color)
    {
        if (!_pixels.TryGetValue(index, out var pixel))
        {
            return;
        }

        if (pixel.IsFinished)
        {
            return;
        }

        pixel.CurrentColor = color;

        _texturePixels[index] = color;
        _imageTexture.SetData(_texturePixels);
    }

    public int GetPixelIndex(PixelData pixelData)
    {
        if (pixelData == null || _imageTexture == null)
        {
            return -1;
        }

        return pixelData.TexturePosition.Y * _imageTexture.Width + pixelData.TexturePosition.X;
    }
    
    public Dictionary<Color, PixelColorGroup> GetPixelColorGroups()
    {
        return _colorGroups;
    }
}