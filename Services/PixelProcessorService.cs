using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Models;

namespace PixelArt.Services;

public class PixelProcessorService
{
    public LevelData CurrentLevel { get; private set; }
    public bool ReplayLaunched { get; private set; }
    
    private float _pixelWidth;
    private float _pixelHeight;
    
    private int _historyIndex;
    private const float _replayDuration = 1f;
    private float _pixelsAccumulator;

    public void Update(GameTime gameTime)
    {
        if (!ReplayLaunched)
        {
            return;
        }

        if (CurrentLevel.History.Count == 0)
        {
            ReplayLaunched = false;
            return;
        }

        var pixelsPerSecond = CurrentLevel.History.Count / _replayDuration;

        _pixelsAccumulator += pixelsPerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds;

        var changed = false;

        var texturePixels = new Color[CurrentLevel.Texture.Width * CurrentLevel.Texture.Height];
        CurrentLevel.Texture.GetData(texturePixels);
        
        while (_pixelsAccumulator >= 1f && _historyIndex < CurrentLevel.History.Count)
        {
            var pixelIndex = CurrentLevel.History[_historyIndex++];
            var pixel = CurrentLevel.Pixels.FirstOrDefault(x => x.Index == pixelIndex);
            if (pixel != null)
            {
                pixel.CurrentColor = pixel.OriginalColor;
                texturePixels[pixelIndex] = pixel.OriginalColor;
                changed = true;
            }

            _pixelsAccumulator -= 1f;
        }

        if (changed)
        {
            CurrentLevel.Texture.SetData(texturePixels);
        }

        if (_historyIndex >= CurrentLevel.History.Count)
        {
            ReplayLaunched = false;
        }
    }
    
    public void ChangeLevel(LevelData levelData)
    {
        CurrentLevel = levelData;
    }

    public void Generate()
    {
        var texturePixels = new Color[CurrentLevel.Texture.Width * CurrentLevel.Texture.Height];
        CurrentLevel.Texture.GetData(texturePixels);
        
        if (CurrentLevel.ColorGroups.Count == 0 && CurrentLevel.Pixels.Count == 0)
        {
            for (var i = 0; i < texturePixels.Length; i++)
            {
                var original = texturePixels[i];

                if (original.A == 0)
                {
                    continue;
                }

                if (CurrentLevel.ColorGroups.All(x => x.OriginalColor != original))
                {
                    CurrentLevel.ColorGroups.Add(new PixelColorGroup
                    {
                        Number = 0,
                        OriginalColor = original,
                        Pixels = []
                    });
                }

                var point = new Point(
                    i % CurrentLevel.Texture.Width,
                    i / CurrentLevel.Texture.Width
                );

                var pixel = new PixelData
                {
                    Index = i,
                    TexturePositionX = point.X,
                    TexturePositionY = point.Y,
                    OriginalColor = original,
                    CurrentColor = Color.White
                };

                CurrentLevel.Pixels.Add(pixel);
                CurrentLevel.ColorGroups.First(x => x.OriginalColor == original).Pixels.Add(pixel);
            }
            
            var sortedGroups = CurrentLevel.ColorGroups
                .OrderByDescending(x => x.Number)
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

                    var index = pixel.TexturePositionY * CurrentLevel.Texture.Width + pixel.TexturePositionX;
                    texturePixels[index] = previewColor;
                }
            }
        }
        else
        {
            CurrentLevel.ColorGroups.ForEach(x => x.Pixels.Clear());
            foreach (var pixel in CurrentLevel.Pixels)
            {
                var index = pixel.TexturePositionY * CurrentLevel.Texture.Width + pixel.TexturePositionX;
                texturePixels[index] = pixel.CurrentColor;
                CurrentLevel.ColorGroups.First(x => x.OriginalColor == pixel.OriginalColor).Pixels.Add(pixel);
            }
        }

        CurrentLevel.Texture.SetData(texturePixels);
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
    
    public void PaintPixelAtMousePosition(MouseState mouse, Color color, CameraService cameraService)
    {
        var bounds = GetImageBounds(cameraService);
        if (bounds.Contains(mouse.Position))
        {
            var x = (int)((mouse.X - bounds.X) / (_pixelWidth * cameraService.Zoom));
            var y = (int)((mouse.Y - bounds.Y) / (_pixelHeight * cameraService.Zoom));
                
            var index = y * CurrentLevel.Texture.Width + x;

            SetPixel(index, color);
        }
    }
    
    public Rectangle GetImageBounds(CameraService cameraService)
    {
        var width = (int)(CurrentLevel.Texture.Width * _pixelWidth * cameraService.Zoom);
        var height = (int)(CurrentLevel.Texture.Height * _pixelHeight * cameraService.Zoom);

        var cameraPosition = cameraService.GetPosition();

        return new Rectangle(
            (int)cameraPosition.X,
            (int)cameraPosition.Y,
            width,
            height
        );
    }

    private void SetPixel(int index, Color color)
    {
        var pixel = CurrentLevel.Pixels.FirstOrDefault(x => x.Index == index);
        
        if (pixel == null)
        {
            return;
        }

        if (pixel.IsFinished)
        {
            return;
        }
        
        if (color == pixel.OriginalColor)
        {
            CurrentLevel.History.Add(index);
        }
        else if (color != pixel.CurrentColor)
        {
            CurrentLevel.ErrorCount++;
        }
        
        var texturePixels = new Color[CurrentLevel.Texture.Width * CurrentLevel.Texture.Height];
        CurrentLevel.Texture.GetData(texturePixels);
        
        pixel.CurrentColor = color;
        texturePixels[index] = color;
        
        CurrentLevel.Texture.SetData(texturePixels);
    }
    
    public void SetPixels(IEnumerable<(int Index, Color Color)> pixels)
    {
        var texturePixels = new Color[CurrentLevel.Texture.Width * CurrentLevel.Texture.Height];
        CurrentLevel.Texture.GetData(texturePixels);
        
        foreach (var (index, color) in pixels)
        {
            var pixel = CurrentLevel.Pixels.FirstOrDefault(x => x.Index == index && !x.IsFinished);
            
            if (pixel == null)
            {
                continue;
            }

            pixel.CurrentColor = color;
            texturePixels[index] = color;
        }

        CurrentLevel.Texture.SetData(texturePixels);
    }

    public int GetPixelIndex(PixelData pixelData)
    {
        if (pixelData == null || CurrentLevel.Texture == null)
        {
            return -1;
        }

        return pixelData.TexturePositionY * CurrentLevel.Texture.Width + pixelData.TexturePositionX;
    }

    public void Draw(SpriteBatch spriteBatch, DrawService drawService, CameraService cameraService)
    {
        var drawBounds = GetImageBounds(cameraService);

        spriteBatch.Draw(
            CurrentLevel.Texture,
            drawBounds,
            Color.White
        );
        
        DrawPixelNumbers(drawBounds, spriteBatch, drawService, cameraService);
    }
    
    private void DrawPixelNumbers(Rectangle bounds, SpriteBatch spriteBatch, DrawService drawService, CameraService cameraService)
    {
        foreach (var colorGroup in CurrentLevel.ColorGroups)
        {
            foreach (var pixel in colorGroup.Pixels.Where(pixel => !pixel.IsFinished))
            {
                var color = Color.Lerp(
                    Color.Transparent,
                    pixel.ColorIsDark() ? Color.White : Color.Black,
                    Utils.Remap(cameraService.Zoom, cameraService.MinZoom, cameraService.MinZoom * 2f, 0f, 1f)
                );
                var scale = cameraService.Zoom + _pixelWidth * (colorGroup.Number.ToString().Length == 1 ? 0.0045f : 0.003f);
                
                drawService.DrawString(
                    spriteBatch,
                    colorGroup.Number.ToString(),
                    pixel.GetScreenPosition(bounds, CurrentLevel.Texture.Width, CurrentLevel.Texture.Height),
                    color,
                    scale
                );
            }
        }
    }

    public void SetPixelSize(float pixelWidth, float pixelHeight)
    {
        _pixelWidth = pixelWidth;
        _pixelHeight = pixelHeight;
    }

    public void Replay()
    {
        var texturePixels = new Color[CurrentLevel.Texture.Width * CurrentLevel.Texture.Height];
        CurrentLevel.Texture.GetData(texturePixels);
        
        foreach (var pixel in CurrentLevel.Pixels)
        {
            pixel.CurrentColor = pixel.GrayColor;
            texturePixels[pixel.Index] = pixel.GrayColor;
        }

        CurrentLevel.Texture.SetData(texturePixels);

        _historyIndex = 0;
        _pixelsAccumulator = 0;
        ReplayLaunched = true;
    }
    
    public void Restart()
    {
        var texturePixels = new Color[CurrentLevel.Texture.Width * CurrentLevel.Texture.Height];
        CurrentLevel.Texture.GetData(texturePixels);

        foreach (var pixel in CurrentLevel.Pixels)
        {
            var index = pixel.TexturePositionY * CurrentLevel.Texture.Width + pixel.TexturePositionX;
            pixel.CurrentColor = pixel.GrayColor;
            texturePixels[index] = pixel.GrayColor;
        }
        
        CurrentLevel.IsFinished = false;
        CurrentLevel.History.Clear();

        CurrentLevel.Texture.SetData(texturePixels);
    }
}