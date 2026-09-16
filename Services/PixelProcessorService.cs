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
    public int BrushRadius { get; set; }

    private Vector2 _pixelSize;
    private int _historyIndex;
    private float _pixelsAccumulator;
    private Point? _lastPaintPixel;
    
    private readonly Color _highlightColor = new(72, 72, 72);
    private readonly HashSet<int> _highlightedPixels = [];
    
    private readonly ParticleService _particleService;
    private readonly CameraService _cameraService;
    private readonly SoundService _soundService;
    private readonly PixelTextureService _textureService;
    private readonly PixelDataService _pixelDataService;

    private float _minNumberPixelSize = 12f;
    private const float _replayDuration = 1.25f;
    
    private readonly Color _glowColor = new(171, 171, 171, 200);
    
    public PixelProcessorService(ParticleService particleService, CameraService cameraService, SoundService soundService, PixelTextureService textureService, PixelDataService pixelDataService)
    {
        _particleService = particleService;
        _cameraService = cameraService;
        _soundService = soundService;
        _textureService = textureService;
        _pixelDataService = pixelDataService;
    }

    public void SetLevel(LevelData levelData)
    {
        CurrentLevel = levelData;

        _minNumberPixelSize = CurrentLevel.Texture.Width / 6f;

        _textureService.SetTexture(CurrentLevel.Texture);

        _pixelDataService.Rebuild(CurrentLevel);

        _highlightedPixels.Clear();
        _lastPaintPixel = null;
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

        UpdateTexture();
    }

    private void ProcessNewImage()
    {
        var width = CurrentLevel.Texture.Width;
        var height = CurrentLevel.Texture.Height;

        var pixels = _textureService.CreateCopy();

        _pixelDataService.BuildNew(pixels, width, height);

        CurrentLevel.Pixels = _pixelDataService.Pixels.ToList();
        CurrentLevel.ColorGroups = _pixelDataService.ColorGroups.ToList();

        var total = CurrentLevel.ColorGroups.Count;

        for (var i = 0; i < total; i++)
        {
            var group = CurrentLevel.ColorGroups[i];
            var grayColor = Utils.GenerateGrayColor(i, total);

            foreach (var pixel in group.Pixels)
            {
                pixel.CurrentColor = new ColorData(grayColor);
                pixel.GrayColor = new ColorData(grayColor);

                _textureService.SetPixel(pixel.Index, grayColor);
            }
        }

        _textureService.Upload();

        CurrentLevel.GrayTexture.SetData(_textureService.CreateCopy());
    }

    private void RebuildExistingImage()
    {
        _pixelDataService.Rebuild(CurrentLevel);

        CurrentLevel.ColorGroups = _pixelDataService.ColorGroups.ToList();

        foreach (var pixel in CurrentLevel.Pixels)
        {
            _textureService.SetPixel(pixel.Index, pixel.CurrentColor.ToColor());
        }

        var size = CurrentLevel.Texture.Width * CurrentLevel.Texture.Height;
        var grayPixels = new Color[size];

        foreach (var pixel in CurrentLevel.Pixels)
        {
            grayPixels[pixel.Index] = pixel.GrayColor.ToColor();
        }

        CurrentLevel.GrayTexture.SetData(grayPixels);
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

        while (_pixelsAccumulator >= 1f && _historyIndex < historyCount)
        {
            var pixelIndex = CurrentLevel.History[_historyIndex++];

            var pixel = _pixelDataService.GetPixel(pixelIndex);

            if (pixel != null)
            {
                pixel.CurrentColor = pixel.OriginalColor;
                _textureService.SetPixel(pixelIndex, pixel.OriginalColor.ToColor());
            }

            _pixelsAccumulator -= 1f;
        }

        if (_historyIndex >= historyCount)
        {
            ReplayLaunched = false;
        }

        UpdateTexture();
    }

    public void Draw(SpriteBatch spriteBatch, DrawService drawService)
    {
        _textureService.Upload();

        var drawBounds = GetImageBounds();

        DrawGlow(spriteBatch, drawBounds);

        spriteBatch.Draw(
            CurrentLevel.Texture,
            drawBounds,
            Color.White);

        DrawPixelNumbers(
            drawBounds,
            spriteBatch,
            drawService);

        _particleService.Draw(spriteBatch);
    }
    
    private void DrawGlow(SpriteBatch spriteBatch, Rectangle bounds)
    {
        const float glowSize = 0.5f;
        const float step = 0.25f;

        var position = new Vector2(bounds.X, bounds.Y);
        var scale = new Vector2((float)bounds.Width / CurrentLevel.GrayTexture.Width,
            (float)bounds.Height / CurrentLevel.GrayTexture.Height);

        for (var i = glowSize; i > 0f; i -= step)
        {
            var offset = i * 2f;

            spriteBatch.Draw(
                CurrentLevel.GrayTexture,
                position + new Vector2(-offset, 0),
                null,
                _glowColor,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);

            spriteBatch.Draw(
                CurrentLevel.GrayTexture,
                position + new Vector2(offset, 0),
                null,
                _glowColor,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);

            spriteBatch.Draw(
                CurrentLevel.GrayTexture,
                position + new Vector2(0, -offset),
                null,
                _glowColor,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);

            spriteBatch.Draw(
                CurrentLevel.GrayTexture,
                position + new Vector2(0, offset),
                null,
                _glowColor,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);
        }
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
                
                var pixel = _pixelDataService.GetPixel(index);

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
        if (!_pixelDataService.TryGetGroup(pixel.OriginalColor.ToColor(), out var colorGroup))
        {
            return;
        }
        
        var multiplier = 2.25f - (32f / CurrentLevel.Texture.Width) * 0.25f;
        
        var color = Color.Lerp(
            Color.Transparent,
            Colors.IsDark(pixel.CurrentColor.ToColor())
                ? Color.White
                : Color.Black,
            Utils.Remap(_cameraService.Zoom, _cameraService.MinZoom, _cameraService.MinZoom * multiplier, 0f, 1f));

        var numberLength = colorGroup.Number.ToString().Length;

        var scale = _cameraService.Zoom + _pixelSize.X * (numberLength == 1 ? 0.004f : 0.0025f);

        if (pixel.CurrentColor != pixel.GrayColor && pixel.CurrentColor.ToColor() != _highlightColor)
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
    }
    
    private void PaintBrush(Point center, Color color)
    {
        var width = CurrentLevel.Texture.Width;
        var height = CurrentLevel.Texture.Height;

        var radiusSquared = BrushRadius * BrushRadius;

        for (var dy = -BrushRadius; dy <= BrushRadius; dy++)
        {
            for (var dx = -BrushRadius; dx <= BrushRadius; dx++)
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
        var pixel = _pixelDataService.GetPixel(index);

        if (pixel == null || pixel.IsFinished)
        {
            return;
        }

        if (color == pixel.OriginalColor.ToColor())
        {
            CurrentLevel.History.Add(index);

            var brightnessOffset = Colors.IsDark(color) ? 40 : -40;

            var particleColor = new Color(
                Math.Clamp(color.R + brightnessOffset, 0, 255),
                Math.Clamp(color.G + brightnessOffset, 0, 255),
                Math.Clamp(color.B + brightnessOffset, 0, 255));

            _particleService.Spawn(pixel.GetWorldPosition(_pixelSize.X, _pixelSize.Y, CurrentLevel.Texture.Width), particleColor, 5);
            _soundService.PlayPaintingSound();

            _highlightedPixels.Remove(index);
        }
        else
        {
            color = Color.Lerp(color, pixel.GrayColor.ToColor(), 0.6f);
        }

        pixel.CurrentColor = new ColorData(color);
        _textureService.SetPixel(index, color);
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
            _textureService.SetPixel(pixel.Index, pixel.GrayColor.ToColor());
        }

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
            _textureService.SetPixel(pixel.Index, pixel.GrayColor.ToColor());
        }

        CurrentLevel.IsFinished = false;
        CurrentLevel.History.Clear();

        _highlightedPixels.Clear();

        UpdateTexture();
    }

    public void ResetPainting()
    {
        _lastPaintPixel = null;
    }

    public void HighlightPixels(Color color)
    {
        ClearHighlight();

        if (!_pixelDataService.TryGetGroup(color, out var selectedGroup))
        {
            return;
        }

        foreach (var pixel in selectedGroup.Pixels.Where(pixel => !pixel.IsFinished))
        {
            pixel.CurrentColor = new ColorData(_highlightColor);
            _textureService.SetPixel(pixel.Index, _highlightColor);

            _highlightedPixels.Add(pixel.Index);
        }
    }

    public void ClearHighlight()
    {
        foreach (var index in _highlightedPixels)
        {
            var pixel = _pixelDataService.GetPixel(index);

            if (pixel == null || pixel.IsFinished)
            {
                continue;
            }

            pixel.CurrentColor = pixel.GrayColor;
            _textureService.SetPixel(index, pixel.GrayColor.ToColor());
        }

        _highlightedPixels.Clear();
    }

    public void UpdateTexture()
    {
        _textureService.Upload();
    }

    private int GetGraphicsWidth()
    {
        return CurrentLevel.Texture.GraphicsDevice.Viewport.Width;
    }

    private int GetGraphicsHeight()
    {
        return CurrentLevel.Texture.GraphicsDevice.Viewport.Height;
    }
    
    public bool HasHighlightedPixelOnScreen()
    {
        if (_highlightedPixels.Count == 0)
        {
            return false;
        }

        var bounds = GetImageBounds();

        var pixelWidth = _pixelSize.X * _cameraService.Zoom;
        var pixelHeight = _pixelSize.Y * _cameraService.Zoom;

        var width = CurrentLevel.Texture.Width;

        foreach (var index in _highlightedPixels)
        {
            var x = index % width;
            var y = index / width;

            var pixelLeft = bounds.X + x * pixelWidth;
            var pixelTop = bounds.Y + y * pixelHeight;
            var pixelRight = pixelLeft + pixelWidth;
            var pixelBottom = pixelTop + pixelHeight;

            if (pixelRight >= 0 && pixelLeft < GetGraphicsWidth() &&
                pixelBottom >= 0 && pixelTop < GetGraphicsHeight())
            {
                return true;
            }
        }

        return false;
    }
    
    public bool TryGetHighlightedPixelScreenPosition(int index, out Vector2 position)
    {
        position = default;

        var pixel = _pixelDataService.GetPixel(index);

        if (pixel == null || pixel.IsFinished)
        {
            return false;
        }

        var bounds = GetImageBounds();

        var pixelWidth = _pixelSize.X * _cameraService.Zoom;
        var pixelHeight = _pixelSize.Y * _cameraService.Zoom;

        var width = CurrentLevel.Texture.Width;

        var x = index % width;
        var y = index / width;

        position = new Vector2(
            bounds.X + (x + 0.5f) * pixelWidth,
            bounds.Y + (y + 0.5f) * pixelHeight);

        return true;
    }
    
    public bool TryGetNearestHighlightedPixel(Vector2 fromPosition, out int index, out Vector2 position)
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
            if (!TryGetHighlightedPixelScreenPosition(pixelIndex, out var pixelPosition))
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

    public bool ContainsHighlightedPixel(int index)
    {
        return _highlightedPixels.Contains(index);
    }
}