using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Models;
using PixelArt.Services.Common;
using PixelArt.Services.Particles;

namespace PixelArt.Services.Pixel;

public class PixelProcessorService
{
    public LevelData CurrentLevel { get; private set; }
    public int BrushRadius { get; set; }

    private Vector2 _pixelSize;
    private Point? _lastPaintPixel;
    
    private readonly ParticleService _particleService;
    private readonly CameraService _cameraService;
    private readonly SoundService _soundService;
    private readonly PixelTextureService _textureService;
    private readonly PixelDataService _pixelDataService;
    private readonly PixelReplayService _replayService;
    private readonly PixelHighlightService _highlightService;

    private float _minNumberPixelSize = 12f;
    
    private readonly Color _glowColor = new(171, 171, 171, 200);
    
    private readonly Dictionary<int, PixelBounceAnimation> _pixelBounceAnimations = new();

    private const float _pixelBounceDuration = 0.18f;
    
    public PixelProcessorService(ParticleService particleService, 
        CameraService cameraService, SoundService soundService, 
        PixelTextureService textureService, PixelDataService pixelDataService, 
        PixelReplayService replayService, PixelHighlightService highlightService)
    {
        _particleService = particleService;
        _cameraService = cameraService;
        _soundService = soundService;
        _textureService = textureService;
        _pixelDataService = pixelDataService;
        _replayService = replayService;
        _highlightService = highlightService;
    }

    public void SetLevel(LevelData levelData)
    {
        CurrentLevel = levelData;

        _minNumberPixelSize = CurrentLevel.Texture.Width / 6f;

        _textureService.SetTexture(CurrentLevel.Texture);

        _pixelDataService.Rebuild(CurrentLevel);

        _highlightService.Reset();
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

        _replayService.Update(CurrentLevel, (float)gameTime.ElapsedGameTime.TotalSeconds, ReplayPixel);

        UpdatePixelBounceAnimations((float)gameTime.ElapsedGameTime.TotalSeconds);

        UpdateTexture();
    }
    
    private void UpdatePixelBounceAnimations(float deltaTime)
    {
        var finished = new List<int>();

        foreach (var animation in _pixelBounceAnimations.ToList())
        {
            var progress = animation.Value.Progress + deltaTime / _pixelBounceDuration;

            if (progress >= 1f)
            {
                finished.Add(animation.Key);
            }
            else
            {
                _pixelBounceAnimations[animation.Key].Progress = progress;
            }
        }

        foreach (var index in finished)
        {
            FinishPixelBounce(index);
        }
    }
    
    private void FinishPixelBounce(int index)
    {
        if (!_pixelBounceAnimations.TryGetValue(index, out var animation))
        {
            return;
        }

        _textureService.SetPixel(index, animation.Color);
        _pixelBounceAnimations.Remove(index);
    }
    
    private void ReplayPixel(int pixelIndex)
    {
        var pixel = _pixelDataService.GetPixel(pixelIndex);

        if (pixel == null)
        {
            return;
        }

        pixel.CurrentColor = pixel.OriginalColor;

        _textureService.SetPixel(pixelIndex, pixel.OriginalColor.ToColor());
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

        DrawPixelBounceAnimations(spriteBatch, drawBounds, drawService);
        
        DrawPixelNumbers(
            drawBounds,
            spriteBatch,
            drawService);

        _particleService.Draw(spriteBatch);
    }
    
    private void DrawPixelBounceAnimations(SpriteBatch spriteBatch, Rectangle bounds, DrawService drawService)
    {
        foreach (var (index, animation) in _pixelBounceAnimations)
        {
            var x = index % CurrentLevel.Texture.Width;
            var y = index / CurrentLevel.Texture.Width;

            var pixelWidth = bounds.Width / (float)CurrentLevel.Texture.Width;
            var pixelHeight = bounds.Height / (float)CurrentLevel.Texture.Height;

            var center = new Vector2(
                bounds.X + x * pixelWidth + pixelWidth / 2f,
                bounds.Y + y * pixelHeight + pixelHeight / 2f);

            var scale = GetBounceScale(animation.Progress);

            var size = new Vector2(pixelWidth * scale, pixelHeight * scale);

            var destination = new Rectangle(
                (int)(center.X - size.X / 2f),
                (int)(center.Y - size.Y / 2f),
                (int)size.X,
                (int)size.Y);

            spriteBatch.Draw(drawService.GetPixelTexture(), destination, animation.Color);
        }
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

        if (pixel.CurrentColor != pixel.GrayColor && !_highlightService.IsHighlighted(pixel.Index))
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

            _highlightService.Remove(index);
            
            _pixelBounceAnimations[index] = new PixelBounceAnimation
            {
                Color = color,
                Progress = 0f
            };
        }
        else
        {
            color = Color.Lerp(color, pixel.GrayColor.ToColor(), 0.6f);
            _textureService.SetPixel(index, color);
        }

        pixel.CurrentColor = new ColorData(color);
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

        _replayService.Start();
        UpdateTexture();
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

        _highlightService.Reset();

        UpdateTexture();
    }

    public void ResetPainting()
    {
        _lastPaintPixel = null;
    }

    public void HighlightPixels(Color color)
    {
        if (!_pixelDataService.TryGetGroup(color, out var selectedGroup))
        {
            return;
        }

        _highlightService.Highlight(
            selectedGroup,
            _pixelDataService,
            pixel => _textureService.SetPixel(
                pixel.Index,
                pixel.CurrentColor.ToColor()));
    }

    public void ClearHighlight()
    {
        _highlightService.Clear(
            _pixelDataService,
            pixel => _textureService.SetPixel(
                pixel.Index,
                pixel.CurrentColor.ToColor()));
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
        return _highlightService.HasPixelOnScreen(
            GetImageBounds(),
            _pixelSize,
            _cameraService.Zoom,
            CurrentLevel.Texture.Width,
            GetGraphicsWidth(),
            GetGraphicsHeight());
    }
    
    public bool TryGetHighlightedPixelScreenPosition(int index, out Vector2 position)
    {
        return _highlightService.TryGetScreenPosition(
            index,
            _pixelDataService,
            GetImageBounds(),
            _pixelSize,
            _cameraService.Zoom,
            CurrentLevel.Texture.Width,
            out position);
    }

    public bool TryGetNearestHighlightedPixel(Vector2 fromPosition, out int index, out Vector2 position)
    {
        return _highlightService.TryGetNearestScreenPosition(fromPosition, _pixelDataService, GetImageBounds(),
            _pixelSize, _cameraService.Zoom, CurrentLevel.Texture.Width, out index, out position);
    }

    public bool ContainsHighlightedPixel(int index)
    {
        return _highlightService.Contains(index);
    }
    
    private float GetBounceScale(float progress)
    {
        if (progress < 0.8f)
        {
            var t1 = progress / 0.8f;
            return MathHelper.Lerp(0f, 1.04f, EaseOutSine(t1));
        }

        var t2 = (progress - 0.8f) / 0.2f;
        return MathHelper.Lerp(1.04f, 1f, EaseOutSine(t2));
    }
    
    private float EaseOutSine(float t)
    {
        return MathF.Sin((t * MathF.PI) / 2);
    }

    public bool PixelIsDark(Vector2 position)
    {
        var bounds = GetImageBounds();

        if (!bounds.Contains(position))
        {
            return false;
        }

        var screenPixelWidth = _pixelSize.X * _cameraService.Zoom;
        var screenPixelHeight = _pixelSize.Y * _cameraService.Zoom;

        var x = (int)((position.X - bounds.X) / screenPixelWidth);
        var y = (int)((position.Y - bounds.Y) / screenPixelHeight);

        if (x < 0 || x >= CurrentLevel.Texture.Width ||
            y < 0 || y >= CurrentLevel.Texture.Height)
        {
            return false;
        }

        var pixel = _pixelDataService.GetPixel(y * CurrentLevel.Texture.Width + x);

        return Colors.IsDark(pixel.CurrentColor.ToColor());
    }
}