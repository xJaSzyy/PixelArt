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
    private float _pixelsAccumulator;
    private Point? _lastPaintPixel;
    private Color[] _texturePixels;
    private PixelData[] _pixelLookup;
    private readonly Color _highlightColor = new(72, 72, 72);
    private readonly HashSet<int> _highlightedPixels = [];
    private bool _textureDirty;
    private readonly Dictionary<Color, PixelColorGroup> _groupsByColor = [];
    
    private readonly ParticleService _particleService;
    private readonly CameraService _cameraService;
    private readonly SoundService _soundService;
    
    private const float _minNumberPixelSize = 6f;
    private const int _brushRadius = 0;
    private const float _replayDuration = 1.25f;

    public PixelProcessorService(ParticleService particleService, CameraService cameraService, SoundService soundService)
    {
        _particleService = particleService;
        _cameraService = cameraService;
        _soundService = soundService;
    }

    public void SetLevel(LevelData levelData)
    {
        CurrentLevel = levelData;

        var size = CurrentLevel.Texture.Width * CurrentLevel.Texture.Height;

        _texturePixels = new Color[size];

        CurrentLevel.Texture.GetData(_texturePixels);

        _pixelLookup = new PixelData[size];

        foreach (var pixel in CurrentLevel.Pixels)
        {
            if (pixel.Index >= 0 && pixel.Index < _pixelLookup.Length)
            {
                _pixelLookup[pixel.Index] = pixel;
            }
        }

        _groupsByColor.Clear();

        foreach (var group in CurrentLevel.ColorGroups)
        {
            _groupsByColor[group.OriginalColor] = group;
        }

        _highlightedPixels.Clear();
        _lastPaintPixel = null;
        _textureDirty = false;
    }

    public void ProcessImage()
    {
        if (CurrentLevel.Pixels.Count == 0)
        {
            ProcessNewImage();
        }
        else
        {
            RebuildExistingImage();
        }

        _textureDirty = true;
        UpdateTexture();
    }

    private void ProcessNewImage()
    {
        CurrentLevel.ColorGroups.Clear();
        CurrentLevel.Pixels.Clear();

        _groupsByColor.Clear();

        var width = CurrentLevel.Texture.Width;
        var height = CurrentLevel.Texture.Height;

        _pixelLookup = new PixelData[width * height];

        for (var i = 0; i < _texturePixels.Length; i++)
        {
            var original = _texturePixels[i];

            if (original.A != 255)
                continue;

            if (!_groupsByColor.TryGetValue(original, out var group))
            {
                group = new PixelColorGroup
                {
                    Number = 0,
                    OriginalColor = original,
                    Pixels = []
                };

                _groupsByColor.Add(original, group);
                CurrentLevel.ColorGroups.Add(group);
            }

            var x = i % width;
            var y = i / width;

            var pixel = new PixelData
            {
                Index = i,

                TexturePositionX = x,
                TexturePositionY = y,

                OriginalColor = original,

                CurrentColor = Color.White
            };

            CurrentLevel.Pixels.Add(pixel);

            _pixelLookup[i] = pixel;

            group.Pixels.Add(pixel);
        }

        var total = CurrentLevel.ColorGroups.Count;

        for (var i = 0; i < total; i++)
        {
            var group = CurrentLevel.ColorGroups[i];

            var grayColor = Utils.GenerateGrayColor(i, total);

            group.Number = i + 1;

            foreach (var pixel in group.Pixels)
            {
                pixel.CurrentColor = grayColor;
                pixel.GrayColor = grayColor;

                _texturePixels[pixel.Index] = grayColor;
            }
        }
    }

    private void RebuildExistingImage()
    {
        _groupsByColor.Clear();

        foreach (var group in CurrentLevel.ColorGroups)
        {
            group.Pixels.Clear();
            _groupsByColor[group.OriginalColor] = group;
        }

        foreach (var pixel in CurrentLevel.Pixels)
        {
            _pixelLookup[pixel.Index] = pixel;

            _texturePixels[pixel.Index] = pixel.CurrentColor;

            if (_groupsByColor.TryGetValue(pixel.OriginalColor, out var group))
            {
                group.Pixels.Add(pixel);
            }
        }
    }

    public void Update(GameTime gameTime)
    {
        _particleService.Update(gameTime);

        if (!ReplayLaunched)
        {
            return;
        }

        var historyCount = CurrentLevel.History.Count;

        if (historyCount == 0)
        {
            ReplayLaunched = false;
            return;
        }

        var pixelsPerSecond = historyCount / _replayDuration;

        _pixelsAccumulator += pixelsPerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds;

        var changed = false;

        while (_pixelsAccumulator >= 1f && _historyIndex < historyCount)
        {
            var pixelIndex = CurrentLevel.History[_historyIndex++];

            if (pixelIndex >= 0 && pixelIndex < _pixelLookup.Length)
            {
                var pixel = _pixelLookup[pixelIndex];

                if (pixel != null)
                {
                    pixel.CurrentColor = pixel.OriginalColor;
                    _texturePixels[pixelIndex] = pixel.OriginalColor;

                    changed = true;
                }
            }

            _pixelsAccumulator -= 1f;
        }

        if (changed)
        {
            _textureDirty = true;
        }

        if (_historyIndex >= historyCount)
        {
            ReplayLaunched = false;
        }

        UpdateTexture();
    }

    public void Draw(SpriteBatch spriteBatch, DrawService drawService)
    {
        UpdateTexture();

        var drawBounds = GetImageBounds();

        spriteBatch.Draw(CurrentLevel.Texture, drawBounds, Color.White);

        DrawPixelNumbers(drawBounds, spriteBatch, drawService);

        _particleService.Draw(spriteBatch);
    }

    private void DrawPixelNumbers(Rectangle bounds, SpriteBatch spriteBatch, DrawService drawService)
    {
        if (CurrentLevel.Pixels.Count == 0)
        {
            return;
        }

        var screenPixelWidth = _pixelSize.X * _cameraService.Zoom;
        var screenPixelHeight = _pixelSize.Y * _cameraService.Zoom;

        if (screenPixelWidth < _minNumberPixelSize || screenPixelHeight < _minNumberPixelSize)
        {
            return;
        }

        var width = CurrentLevel.Texture.Width;
        var height = CurrentLevel.Texture.Height;

        var minX = Math.Max(0, (int)(-bounds.X / screenPixelWidth));
        var minY = Math.Max(0, (int)(-bounds.Y / screenPixelHeight));
        var maxX = Math.Min(width - 1, (int)((GetGraphicsWidth() - bounds.X) / screenPixelWidth) + 1);
        var maxY = Math.Min(height - 1, (int)((GetGraphicsHeight() - bounds.Y) / screenPixelHeight) + 1);

        minX = Math.Max(minX, (int)((_cameraService.GetPosition().X - bounds.X) / screenPixelWidth));

        for (var y = minY; y <= maxY; y++)
        {
            var rowStart = y * width;

            for (var x = minX; x <= maxX; x++)
            {
                var index = rowStart + x;

                var pixel = _pixelLookup[index];

                if (pixel == null || pixel.IsFinished)
                {
                    continue;
                }

                DrawPixelNumber(pixel, bounds, spriteBatch, drawService);
            }
        }
    }

    private void DrawPixelNumber(PixelData pixel, Rectangle bounds, SpriteBatch spriteBatch, DrawService drawService)
    {
        if (!_groupsByColor.TryGetValue(pixel.OriginalColor, out var colorGroup))
        {
            return;
        }

        var color = Color.Lerp(
            Color.Transparent,
            Colors.IsDark(pixel.CurrentColor)
                ? Color.White
                : Color.Black,
            Utils.Remap(_cameraService.Zoom, _cameraService.MinZoom, _cameraService.MinZoom * 2f, 0f, 1f));

        var numberLength = colorGroup.Number.ToString().Length;

        var scale = _cameraService.Zoom + _pixelSize.X * (numberLength == 1 ? 0.0045f : 0.003f);

        if (pixel.CurrentColor != pixel.GrayColor && pixel.CurrentColor != _highlightColor)
        {
            color *= .6f;
        }

        drawService.DrawString(
            spriteBatch, 
            colorGroup.Number.ToString(), 
            pixel.GetScreenPosition(bounds, CurrentLevel.Texture.Width, CurrentLevel.Texture.Height), 
            color, 
            scale);
    }

    public void PaintAtMousePosition(MouseState mouse, Color color)
    {
        var bounds = GetImageBounds();

        if (!bounds.Contains(mouse.Position))
        {
            _lastPaintPixel = null;
            return;
        }

        var screenPixelWidth = _pixelSize.X * _cameraService.Zoom;
        var screenPixelHeight = _pixelSize.Y * _cameraService.Zoom;

        var x = (int)((mouse.X - bounds.X) / screenPixelWidth);
        var y = (int)((mouse.Y - bounds.Y) / screenPixelHeight);

        if (x < 0 || x >= CurrentLevel.Texture.Width ||
            y < 0 || y >= CurrentLevel.Texture.Height)
        {
            _lastPaintPixel = null;
            return;
        }

        var currentPixel = new Point(x, y);

        if (_lastPaintPixel.HasValue)
        {
            foreach (var point in Utils.GetLine(_lastPaintPixel.Value, currentPixel))
            {
                PaintBrush(point, color);
            }
        }
        else
        {
            PaintBrush(currentPixel, color);
        }

        _lastPaintPixel = currentPixel;

        _textureDirty = true;
    }
    

    private void PaintBrush(Point center, Color color)
    {
        var width = CurrentLevel.Texture.Width;
        var height = CurrentLevel.Texture.Height;

        var radiusSquared = _brushRadius * _brushRadius;

        for (var dy = -_brushRadius; dy <= _brushRadius; dy++)
        {
            for (var dx = -_brushRadius; dx <= _brushRadius; dx++)
            {
                if (dx * dx + dy * dy > radiusSquared)
                {
                    continue;
                }

                var x = center.X + dx;
                var y = center.Y + dy;

                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    continue;
                }

                SetPixel(y * width + x, color);
            }
        }
    }

    public Rectangle GetImageBounds()
    {
        var zoom = _cameraService.Zoom;

        var width = (int)(CurrentLevel.Texture.Width * _pixelSize.X * zoom);
        var height = (int)(CurrentLevel.Texture.Height * _pixelSize.Y * zoom);

        var cameraPosition = _cameraService.GetPosition();

        return new Rectangle((int)cameraPosition.X, (int)cameraPosition.Y, width, height);
    }

    private void SetPixel(int index, Color color)
    {
        if (index < 0 || index >= _pixelLookup.Length)
        {
            return;
        }

        var pixel = _pixelLookup[index];

        if (pixel == null || pixel.IsFinished)
        {
            return;
        }

        if (color == pixel.OriginalColor)
        {
            CurrentLevel.History.Add(index);

            var brightnessOffset = Colors.IsDark(color) ? 40 : -40;

            var particleColor = new Color(
                Math.Clamp(color.R + brightnessOffset, 0, 255),
                Math.Clamp(color.G + brightnessOffset, 0, 255),
                Math.Clamp(color.B + brightnessOffset, 0, 255));

            _particleService.Spawn(pixel.GetWorldPosition(_pixelSize.X, _pixelSize.Y), particleColor, 5);
            _soundService.PlayPaintingSound();
        }
        else
        {
            color = Color.Lerp(color, pixel.GrayColor, 0.6f);
        }

        pixel.CurrentColor = color;
        _texturePixels[index] = color;

        _textureDirty = true;
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

        _textureDirty = true;

        UpdateTexture();

        _historyIndex = 0;
        _pixelsAccumulator = 0;

        ReplayLaunched = true;
    }

    public void Restart()
    {
        foreach (var pixel in CurrentLevel.Pixels)
        {
            pixel.CurrentColor = pixel.GrayColor;
            _texturePixels[pixel.Index] = pixel.GrayColor;
        }

        CurrentLevel.IsFinished = false;
        CurrentLevel.History.Clear();

        _highlightedPixels.Clear();

        _textureDirty = true;

        UpdateTexture();
    }

    public void ResetPainting()
    {
        _lastPaintPixel = null;
    }

    public void HighlightPixels(Color color)
    {
        ClearHighlight();

        if (!_groupsByColor.TryGetValue(color, out var selectedGroup))
        {
            return;
        }

        foreach (var pixel in selectedGroup.Pixels.Where(pixel => !pixel.IsFinished))
        {
            pixel.CurrentColor = _highlightColor;
            _texturePixels[pixel.Index] = _highlightColor;

            _highlightedPixels.Add(pixel.Index);
        }

        _textureDirty = true;
    }

    public void ClearHighlight()
    {
        foreach (var index in _highlightedPixels)
        {
            if (index < 0 || index >= _pixelLookup.Length)
            {
                continue;
            }

            var pixel = _pixelLookup[index];

            if (pixel == null || pixel.IsFinished)
            {
                continue;
            }

            pixel.CurrentColor = pixel.GrayColor;
            _texturePixels[index] = pixel.GrayColor;
        }

        _highlightedPixels.Clear();

        _textureDirty = true;
    }

    public void UpdateTexture()
    {
        if (!_textureDirty)
        {
            return;
        }

        CurrentLevel.Texture.SetData(_texturePixels);

        _textureDirty = false;
    }

    private int GetGraphicsWidth()
    {
        return CurrentLevel.Texture.GraphicsDevice.Viewport.Width;
    }

    private int GetGraphicsHeight()
    {
        return CurrentLevel.Texture.GraphicsDevice.Viewport.Height;
    }
}