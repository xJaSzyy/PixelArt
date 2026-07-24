using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Models;

namespace PixelArt.Services;

public class PixelProcessorService
{
    private Texture2D _imageTexture;
    private readonly Dictionary<Color, PixelColorGroup> _colorMap = new();
    private readonly Dictionary<int, PixelData> _pixels = [];
    private Color[] _texturePixels;

    public void SetTexture(Texture2D texture)
    {
        _imageTexture = texture;
    }
    
    public void Generate()
    {
        _texturePixels = new Color[_imageTexture.Width * _imageTexture.Height];
        var width = _imageTexture.Width;
        _imageTexture.GetData(_texturePixels);
        
        _colorMap.Clear();
        _pixels.Clear();
        
        for (var i = 0; i < _texturePixels.Length; i++)
        {
            Color original = _texturePixels[i];

            if (original.A == 0)
            {
                continue;
            }
            
            byte grayValue = (byte)(
                original.R * 0.299f +
                original.G * 0.587f +
                original.B * 0.114f
            );

            grayValue = (byte)(50 + grayValue * 0.8f);

            var gray = new Color(
                grayValue,
                grayValue,
                grayValue,
                original.A
            );
            
            _texturePixels[i] = gray;
            
            if (!_colorMap.ContainsKey(original))
            {
                _colorMap[original] = new PixelColorGroup
                {
                    Number = _colorMap.Count + 1,
                    OriginalColor = original,
                    GrayColor = gray,
                    Pixels = []
                };
            }

            var point = new Point(i % width, i / width);

            var pixel = new PixelData
            {
                Index = i,
                TexturePosition = point,
                OriginalColor = original,
                CurrentColor = gray
            };
            
            _pixels[i] = pixel;
            _colorMap[original].Pixels.Add(pixel);
        }
        
        _imageTexture.SetData(_texturePixels);
    }

    public void SetPixel(int index, Color color)
    {
        if (!_pixels.TryGetValue(index, out var pixel))
        {
            return;
        }

        pixel.CurrentColor = color;
        _texturePixels[index] = color;

        _imageTexture.SetData(_texturePixels);
    }

    public Dictionary<Color, PixelColorGroup> GetColorMap()
    {
        return _colorMap;
    }
}