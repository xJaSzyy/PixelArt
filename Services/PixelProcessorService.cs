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

    public void SetTexture(Texture2D texture)
    {
        _imageTexture = texture;
    }
    
    public void Generate()
    {
        var pixels = new Color[_imageTexture.Width * _imageTexture.Height];
        var width = _imageTexture.Width;
        _imageTexture.GetData(pixels);
        
        _colorMap.Clear();
        
        for (var i = 0; i < pixels.Length; i++)
        {
            Color original = pixels[i];

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
            
            pixels[i] = gray;
            
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
            
            _colorMap[original].Pixels.Add(new PixelData
            {
                Index = i,
                TexturePosition = point,
            });
        }
        
        _imageTexture.SetData(pixels);
    }

    public Dictionary<Color, PixelColorGroup> GetColorMap()
    {
        return _colorMap;
    }
}