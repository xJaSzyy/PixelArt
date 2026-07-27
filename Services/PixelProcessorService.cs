using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;
using PixelArt.Models;

namespace PixelArt.Services;

public class PixelProcessorService(CameraService cameraService)
{
    private Texture2D _imageTexture;
    private readonly Dictionary<Color, PixelColorGroup> _colorGroups = new();
    private readonly Dictionary<int, PixelData> _pixels = [];
    private Color[] _texturePixels;
    
    private float _pixelWidth;
    private float _pixelHeight;
    
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
        
        const int min = 150;
        const int max = 230;
        
        var value = max -
                    index * (max - min) /
                    (total - 1);
        
        return (byte)value;
    }
    
    public void PaintPixelAtMousePosition(MouseState mouse, ColorButton selectedButton)
    {
        
        var bounds = GetImageBounds();
        if (selectedButton != null && bounds.Contains(mouse.Position))
        {
            var x = (int)((mouse.X - bounds.X) / (_pixelWidth * cameraService.Zoom));
            var y = (int)((mouse.Y - bounds.Y) / (_pixelHeight * cameraService.Zoom));
                
            var index = y * _imageTexture.Width + x;

            SetPixel(index, selectedButton.Color);
        }
    }
    
    private Rectangle GetImageBounds()
    {
        var width = (int)(_imageTexture.Width * _pixelWidth * cameraService.Zoom);
        var height = (int)(_imageTexture.Height * _pixelHeight * cameraService.Zoom);

        var cameraPosition = cameraService.GetPosition();
        
        return new Rectangle(
            (int)cameraPosition.X,
            (int)cameraPosition.Y,
            width,
            height
        );
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

    public void Draw(SpriteBatch spriteBatch, DrawService drawService)
    {
        var drawBounds = GetImageBounds();

        spriteBatch.Draw(
            _imageTexture,
            drawBounds,
            Color.White
        );
        
        DrawPixelNumbers(drawBounds, spriteBatch, drawService);
    }
    
    private void DrawPixelNumbers(Rectangle bounds, SpriteBatch spriteBatch, DrawService drawService)
    {
        foreach (var color in _colorGroups.Values)
        {
            foreach (var pixel in color.Pixels.Where(pixel => !pixel.IsFinished))
            {
                drawService.DrawString(
                    spriteBatch, 
                    color.Number.ToString(), 
                    pixel.GetScreenPosition(bounds, _imageTexture.Width, _imageTexture.Height), 
                    Color.Lerp(
                        Color.Transparent,
                        pixel.ColorIsDark() ? Color.White : Color.Black,
                        GetNumberAlpha()
                    ),
                    cameraService.Zoom);
            }
        }
    }

    public float GetNumberAlpha()
    {
        var zoom = cameraService.Zoom;

        return MathHelper.Clamp(
            (zoom - cameraService.MinZoom) / cameraService.MinZoom,
            0,
            1
        );
    }

    public void SetPixelSize(float pixelWidth, float pixelHeight)
    {
        _pixelWidth = pixelWidth;
        _pixelHeight = pixelHeight;
    }
    
    public Dictionary<Color, PixelColorGroup> GetPixelColorGroups()
    {
        return _colorGroups;
    }
}