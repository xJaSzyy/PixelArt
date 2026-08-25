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
    
    private Vector2 _pixelSize;
    
    private int _historyIndex;
    private const float _replayDuration = 1.25f;
    private float _pixelsAccumulator;
    
    private Point? _lastPaintPixel;
    private Color[] _texturePixels;
    private readonly Color _highlightColor = new(72, 72, 72);
    private readonly HashSet<int> _highlightedPixels = [];
    private Dictionary<int, PixelData> _pixelsByIndex;
    
    private readonly ParticleService _particleService;
    private readonly CameraService _cameraService;
    private readonly SoundService _soundService;

    public PixelProcessorService(ParticleService particleService, CameraService cameraService, SoundService soundService)
    {
        _particleService = particleService;
        _cameraService = cameraService;
        _soundService = soundService;
    }
    
    public void SetLevel(LevelData levelData)
        {
            CurrentLevel = levelData;
            
            _pixelsByIndex = CurrentLevel.Pixels
                .ToDictionary(x => x.Index);
            
            _texturePixels = new Color[CurrentLevel.Texture.Width * CurrentLevel.Texture.Height];
            CurrentLevel.Texture.GetData(_texturePixels);
        }
    
    public void ProcessImage()
        {
            if (CurrentLevel.ColorGroups.Count == 0 && CurrentLevel.Pixels.Count == 0)
            {
                for (var i = 0; i < _texturePixels.Length; i++)
                {
                    var original = _texturePixels[i];
    
                    if (original.A != 255)
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
                        _texturePixels[index] = previewColor;
                    }
                }
            }
            else
            {
                CurrentLevel.ColorGroups.ForEach(x => x.Pixels.Clear());
                foreach (var pixel in CurrentLevel.Pixels)
                {
                    var index = pixel.TexturePositionY * CurrentLevel.Texture.Width + pixel.TexturePositionX;
                    _texturePixels[index] = pixel.CurrentColor;
                    CurrentLevel.ColorGroups.First(x => x.OriginalColor == pixel.OriginalColor).Pixels.Add(pixel);
                }
            }
    
            CurrentLevel.Texture.SetData(_texturePixels);
        }
    
    public void Update(GameTime gameTime)
    {
        _particleService.Update(gameTime);
        
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
            var pixel = CurrentLevel.Pixels.FirstOrDefault(x => x.Index == pixelIndex);
            if (pixel != null)
            {
                pixel.CurrentColor = pixel.OriginalColor;
                _texturePixels[pixelIndex] = pixel.OriginalColor;
                changed = true;
            }

            _pixelsAccumulator -= 1f;
        }

        if (changed)
        {
            CurrentLevel.Texture.SetData(_texturePixels);
        }

        if (_historyIndex >= CurrentLevel.History.Count)
        {
            ReplayLaunched = false;
        }
    }
    
    public void Draw(SpriteBatch spriteBatch, DrawService drawService)
    {
        CurrentLevel.Texture.SetData(_texturePixels);
        
        var drawBounds = GetImageBounds();

        spriteBatch.Draw(
            CurrentLevel.Texture,
            drawBounds,
            Color.White
        );
        
        DrawPixelNumbers(drawBounds, spriteBatch, drawService);
        
        _particleService.Draw(spriteBatch);
    }
    
    private void DrawPixelNumbers(Rectangle bounds, SpriteBatch spriteBatch, DrawService drawService)
    {
        foreach (var colorGroup in CurrentLevel.ColorGroups)
        {
            foreach (var pixel in colorGroup.Pixels.Where(pixel => !pixel.IsFinished))
            {
                var color = Color.Lerp(
                    Color.Transparent,
                    Colors.IsDark(pixel.CurrentColor) ? Color.White : Color.Black,
                    Utils.Remap(_cameraService.Zoom, _cameraService.MinZoom, _cameraService.MinZoom * 2f, 0f, 1f)
                );
                var scale = _cameraService.Zoom + _pixelSize.X * (colorGroup.Number.ToString().Length == 1 ? 0.0045f : 0.003f);
                
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
    
    private static byte GenerateGrayValue(int index, int total)
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
    
    public void PaintAtMousePosition(MouseState mouse, Color color)
    {
        var bounds = GetImageBounds();

        if (!bounds.Contains(mouse.Position))
        {
            _lastPaintPixel = null;
            return;
        }

        var x = (int)((mouse.X - bounds.X) / (_pixelSize.X * _cameraService.Zoom));
        var y = (int)((mouse.Y - bounds.Y) / (_pixelSize.Y * _cameraService.Zoom));

        var currentPixel = new Point(x, y);

        if (_lastPaintPixel.HasValue)
        {
            PaintLine(_lastPaintPixel.Value, currentPixel, color);
        }
        else
        {
            SetPixel(y * CurrentLevel.Texture.Width + x, color);
        }

        _lastPaintPixel = currentPixel;
    }
    
    private void PaintLine(Point start, Point end, Color color)
    {
        var x0 = start.X;
        var y0 = start.Y;

        var x1 = end.X;
        var y1 = end.Y;

        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);

        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;

        var err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < CurrentLevel.Texture.Width &&
                y0 >= 0 && y0 < CurrentLevel.Texture.Height)
            {
                SetPixel(y0 * CurrentLevel.Texture.Width + x0, color);
            }

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var e2 = 2 * err;

            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
    
    public Rectangle GetImageBounds()
    {
        var width = (int)(CurrentLevel.Texture.Width * _pixelSize.X * _cameraService.Zoom);
        var height = (int)(CurrentLevel.Texture.Height * _pixelSize.Y * _cameraService.Zoom);

        var cameraPosition = _cameraService.GetPosition();

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
        
        pixel.CurrentColor = color;
        _texturePixels[index] = color;
        
        if (color == pixel.OriginalColor)
        {
            CurrentLevel.History.Add(index);
            
            var l = Colors.IsDark(color) ? 40 : -40;
            var particleColor = new Color(
                Math.Min(color.R + l, 255),
                Math.Min(color.G + l, 255),
                Math.Min(color.B + l, 255)
            );

            _particleService.Spawn(pixel.GetWorldPosition(_pixelSize.X, _pixelSize.Y), particleColor, 5);
            _soundService.PlayPaintingSound();
        }
    }

    public void SetPixelSize(float pixelWidth, float pixelHeight)
    {
        _pixelSize = new Vector2(pixelWidth, pixelHeight);
    }

    public void Replay()
    {
        foreach (var pixel in CurrentLevel.Pixels)
        {
            pixel.CurrentColor = pixel.GrayColor;
            _texturePixels[pixel.Index] = pixel.GrayColor;
        }

        CurrentLevel.Texture.SetData(_texturePixels);

        _historyIndex = 0;
        _pixelsAccumulator = 0;
        ReplayLaunched = true;
    }
    
    public void Restart()
    {
        foreach (var pixel in CurrentLevel.Pixels)
        {
            var index = pixel.TexturePositionY * CurrentLevel.Texture.Width + pixel.TexturePositionX;
            pixel.CurrentColor = pixel.GrayColor;
            _texturePixels[index] = pixel.GrayColor;
        }
        
        CurrentLevel.IsFinished = false;
        CurrentLevel.History.Clear();

        CurrentLevel.Texture.SetData(_texturePixels);
    }

    public void ResetPainting()
    {
        _lastPaintPixel = null;
    }
    
    public void HighlightPixels(Color color)
    {
        ClearHighlight();

        var selectedGroup = CurrentLevel.ColorGroups
            .FirstOrDefault(x => x.OriginalColor == color);

        if (selectedGroup == null)
        {
            return;
        }

        foreach (var pixel in selectedGroup.Pixels.Where(pixel => !pixel.IsFinished))
        {
            pixel.CurrentColor = _highlightColor;
            _texturePixels[pixel.Index] = _highlightColor;

            _highlightedPixels.Add(pixel.Index);
        }
    }

    public void ClearHighlight()
    {
        foreach (var index in _highlightedPixels)
        {
            if (!_pixelsByIndex.TryGetValue(index, out var pixel) || pixel.IsFinished)
            {
                continue;
            }

            pixel.CurrentColor = pixel.GrayColor;
            _texturePixels[index] = pixel.GrayColor;
        }

        _highlightedPixels.Clear();
    }
}