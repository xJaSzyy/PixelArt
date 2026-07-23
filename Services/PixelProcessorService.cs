using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Models;

namespace PixelArt.Services;

public class PixelProcessorService
{
    private readonly Texture2D _imageTexture;
    private readonly Dictionary<Color, List<int>> _colorMap = new();

    public PixelProcessorService(Texture2D imageTexture)
    {
        _imageTexture =  imageTexture;
    }
    
    public void GenerateColorMap()
    {
        var pixels = new Color[_imageTexture.Width * _imageTexture.Height];
        _imageTexture.GetData(pixels);

        _colorMap.Clear();

        for (var i = 0; i < pixels.Length; i++)
        {
            var color = pixels[i];

            if (color.A == 0)
            {
                continue;
            }
            
            if (!_colorMap.ContainsKey(color))
            {
                _colorMap[color] = [];
            }

            _colorMap[color].Add(i);
        }
    }
    
    public void GenerateGrayImage()
    {
        var pixels = new Color[_imageTexture.Width * _imageTexture.Height];
        _imageTexture.GetData(pixels);

        for (int i = 0; i < pixels.Length; i++)
        {
            Color original = pixels[i];

            byte grayValue = (byte)(
                original.R * 0.299f +
                original.G * 0.587f +
                original.B * 0.114f
            );

            Color gray = new Color(
                grayValue,
                grayValue,
                grayValue,
                original.A
            );

            pixels[i] = gray;
        }

        _imageTexture.SetData(pixels);
    }
}