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

        while (_pixelsAccumulator >= 1f && _historyIndex < CurrentLevel.History.Count)
        {
            var pixelIndex = CurrentLevel.History[_historyIndex++];

            if (CurrentLevel.Pixels.TryGetValue(pixelIndex, out var pixel))
            {
                pixel.CurrentColor = pixel.OriginalColor;
                CurrentLevel.TexturePixels[pixelIndex] = pixel.OriginalColor;
                changed = true;
            }

            _pixelsAccumulator -= 1f;
        }

        if (changed)
        {
            CurrentLevel.Texture.SetData(CurrentLevel.TexturePixels);
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
        CurrentLevel.TexturePixels = new Color[CurrentLevel.Texture.Width * CurrentLevel.Texture.Height];
        CurrentLevel.Texture.GetData(CurrentLevel.TexturePixels);

        CurrentLevel.ColorGroups.Clear();
        CurrentLevel.Pixels.Clear();
        
        for (var i = 0; i < CurrentLevel.TexturePixels.Length; i++)
        {
            var original = CurrentLevel.TexturePixels[i];

            if (original.A == 0)
            {
                continue;
            }
            
            if (!CurrentLevel.ColorGroups.ContainsKey(original))
            {
                CurrentLevel.ColorGroups[original] = new PixelColorGroup
                {
                    Number = 0,
                    OriginalColor = original,
                    Pixels = []
                };
            }
            
            var point = new Point(
                i % CurrentLevel.Texture.Width,
                i / CurrentLevel.Texture.Width
            );

            var pixel = new PixelData
            {
                Index = i,
                TexturePosition = point,
                OriginalColor = original,
                CurrentColor = Color.White
            };

            CurrentLevel.Pixels[i] = pixel;
            CurrentLevel.ColorGroups[original].Pixels.Add(pixel);
        }
        
        var sortedGroups = CurrentLevel.ColorGroups.Values
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
                    pixel.TexturePosition.Y * CurrentLevel.Texture.Width +
                    pixel.TexturePosition.X;

                CurrentLevel.TexturePixels[index] = previewColor;
            }
        }

        CurrentLevel.Texture.SetData(CurrentLevel.TexturePixels);
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
        if (!CurrentLevel.Pixels.TryGetValue(index, out var pixel))
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
        
        pixel.CurrentColor = color;
        CurrentLevel.TexturePixels[index] = color;
    }

    public void ApplyPixelChanges()
    {
        CurrentLevel.Texture.SetData(CurrentLevel.TexturePixels);
    }
    
    public void SetPixels(IEnumerable<(int Index, Color Color)> pixels)
    {
        foreach (var (index, color) in pixels)
        {
            if (!CurrentLevel.Pixels.TryGetValue(index, out var pixel))
            {
                continue;
            }

            if (pixel.IsFinished)
            {
                continue;
            }

            pixel.CurrentColor = color;
            CurrentLevel.TexturePixels[index] = color;
        }

        CurrentLevel.Texture.SetData(CurrentLevel.TexturePixels);
    }

    public int GetPixelIndex(PixelData pixelData)
    {
        if (pixelData == null || CurrentLevel.Texture == null)
        {
            return -1;
        }

        return pixelData.TexturePosition.Y * CurrentLevel.Texture.Width + pixelData.TexturePosition.X;
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
        foreach (var colorGroup in CurrentLevel.ColorGroups.Values)
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
        foreach (var pixel in CurrentLevel.Pixels)
        {
            pixel.Value.CurrentColor = pixel.Value.GrayColor;
            CurrentLevel.TexturePixels[pixel.Key] = pixel.Value.GrayColor;
        }

        CurrentLevel.Texture.SetData(CurrentLevel.TexturePixels);

        _historyIndex = 0;
        _pixelsAccumulator = 0;
        ReplayLaunched = true;
    }
}