using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Models;

namespace PixelArt.Services;

public class PixelService(GraphicsDevice graphicsDevice, DrawService drawService, CameraService cameraService)
{
    private Vector2 Position { get; set; }
    private Color CurrentColor { get; set; }
    private bool ContourFinished { get; set; }
    
    private readonly Color _highlightColor = new(72, 72, 72);

    private readonly Point _gridSize = new(32, 32);
    private const int _pixelSize = 16;
    
    private ContourService _contourService;
    
    private Texture2D _sourceTexture;
    private Color[] _sourceData;
    private Texture2D _pixelTexture;
    private readonly List<Point> _contour = [];
    private readonly List<Point> _keyPoints = [];
    private Dictionary<Color, int> _colorIndexes;
    private readonly Dictionary<Point, Pixel> _pixelsByPosition = [];

    public void LoadContent(ContentManager content, Texture2D texture)
    {
        _sourceTexture = texture;
        _sourceData = new Color[_sourceTexture.Width * _sourceTexture.Height];
        _sourceTexture.GetData(_sourceData);

        _contourService = new ContourService(_sourceTexture, _sourceData, _gridSize);

        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);

        CurrentColor = Color.Yellow;
        
        Reset();
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        for (var y = 0; y < _gridSize.Y; y++)
        {
            for (var x = 0; x < _gridSize.X; x++)
            {
                var rectangle = GetImageBounds(x, y);

                var pixel = _pixelsByPosition[new Point(x, y)];

                if (pixel == null)
                {
                    continue;
                }

                spriteBatch.Draw(_pixelTexture, rectangle, pixel.CurrentColor);

                if (_keyPoints.Contains(pixel.Position) && !pixel.IsFinished)
                {
                    var text = (_keyPoints.IndexOf(pixel.Position) + 1).ToString();
                    var color = pixel.CurrentColor == Color.Yellow ? Color.Black : Color.Yellow;

                    if (ContourFinished)
                    {
                        text = (_colorIndexes[CurrentColor] + 1).ToString();
                        color = Color.Lerp(
                            Color.Transparent,
                            Colors.IsDark(pixel.CurrentColor) ? Color.White : Color.Black,
                            Utils.Remap(cameraService.Zoom, cameraService.MinZoom, cameraService.MinZoom * 2f, 0f, 1f)
                        );
                    }
                    
                    var scale = cameraService.Zoom * _pixelSize * (text.Length == 1 ? 0.035f : 0.025f);
                    
                    drawService.DrawString(
                        spriteBatch, 
                        text, 
                        rectangle.Center.ToVector2(), 
                        color, 
                        scale);
                }
            }
        }
    }

    private Rectangle GetImageBounds(int x, int y)
    {
        var cameraPos = cameraService.GetPosition();
        var zoom = cameraService.Zoom;

        var worldX = Position.X + x * _pixelSize;
        var worldY = Position.Y + y * _pixelSize;

        var left = (int)MathF.Round(cameraPos.X + worldX * zoom);
        var top = (int)MathF.Round(cameraPos.Y + worldY * zoom);

        var right = (int)MathF.Round(
            cameraPos.X + (worldX + _pixelSize) * zoom);

        var bottom = (int)MathF.Round(
            cameraPos.Y + (worldY + _pixelSize) * zoom);

        return new Rectangle(
            left,
            top,
            right - left,
            bottom - top
        );
    }

    private bool TryGetGridPosition(
        Vector2 screenPosition,
        out int x,
        out int y)
    {
        var cameraPos = cameraService.GetPosition();

        var worldPosition =
            (screenPosition - cameraPos) / cameraService.Zoom;

        var localPosition = worldPosition - Position;

        x = (int)(localPosition.X / _pixelSize);
        y = (int)(localPosition.Y / _pixelSize);

        return x >= 0 &&
               x < _gridSize.X &&
               y >= 0 &&
               y < _gridSize.Y;
    }

    public void PaintAt(Vector2 screenPosition)
    {
        if (!TryGetGridPosition(screenPosition, out var x, out var y))
        {
            return;
        }

        var pixel = _pixelsByPosition[new Point(x, y)];

        if (pixel == null)
        {
            return;
        }

        if (ContourFinished && (pixel.OriginalColor == Color.Transparent || pixel.IsFinished))
        {
            return;
        }

        var currentColor = CurrentColor;
        
        if (pixel.OriginalColor != currentColor && ContourFinished)
        {
            currentColor = Color.Lerp(CurrentColor, pixel.GrayColor, 0.6f);
        }
            
        pixel.CurrentColor = currentColor;
        
        if (ContourFinished && _pixelsByPosition
                .Where(p => p.Value.OriginalColor == CurrentColor)
                .All(p => p.Value.IsFinished))
        {
            ChangeColor();
        }
    }

    public void Center(int viewportWidth, int viewportHeight)
    {
        var width = _gridSize.X * _pixelSize;
        var height = _gridSize.Y * _pixelSize;

        var zoom = cameraService.Zoom;

        var viewportWorldWidth = viewportWidth / zoom;
        var viewportWorldHeight = viewportHeight / zoom;

        Position = new Vector2(
            (viewportWorldWidth - width) / 2f,
            (viewportWorldHeight - height) / 2f
        );
    }

    public void Reset()
    {
        CurrentColor = Color.Yellow;
        
        _contour.Clear();
        _contour.AddRange(_contourService.TraceContour());
        BuildKeyPoints();
        
        _pixelsByPosition.Clear();
        
        ContourFinished = false;
        
        _colorIndexes = new Dictionary<Color, int>();

        for (var y = 0; y < _gridSize.Y; y++)
        {
            for (var x = 0; x < _gridSize.X; x++)
            {
                var index = y * _sourceTexture.Width + x;
                var originalColor = _sourceData[index];

                if (originalColor.A == 255 && !_colorIndexes.ContainsKey(originalColor))
                {
                    _colorIndexes[originalColor] = _colorIndexes.Count;
                }

                var colorIndex = _colorIndexes.GetValueOrDefault(originalColor, -1);

                var pixelToAdd = new Pixel
                {
                    X = x,
                    Y = y,
                    CurrentColor = Color.Transparent,
                    OriginalColor = _sourceData[index],
                    GrayColor = colorIndex >= 0
                        ? Utils.GenerateGrayColor(colorIndex, _colorIndexes.Count)
                        : Color.Transparent
                };
                
                _pixelsByPosition.Add(pixelToAdd.Position, pixelToAdd);
            }
        }
    }

    public void CheckContourMatch(float requiredPercent = 0.98f)
    {
        if (_contour.Count == 0 || ContourFinished)
        {
            return;
        }

        var painted = _pixelsByPosition
            .Where(x => x.Value.CurrentColor.A > 0)
            .Select(x => x.Value.Position)
            .ToHashSet();

        if (painted.Count == 0)
        {
            return;
        }

        var matchedContourPoints = 0;

        foreach (var contourPoint in _contour)
        {
            if (Utils.IsPointNear(contourPoint, painted, 1))
            {
                matchedContourPoints++;
            }
        }

        var contourCoverage = (float)matchedContourPoints / _contour.Count;
        
        var result = contourCoverage >= requiredPercent;

        if (result)
        {
            FinishContour();
        }
    }

    private void FinishContour()
    {
        foreach (var pixel in _pixelsByPosition)
        {
            var color = pixel.Value.GrayColor;
            
            if (_contour.Contains(pixel.Value.Position))
            {
                color = pixel.Value.OriginalColor;
            }

            pixel.Value.CurrentColor = color;
        }
        
        ContourFinished = true;
        _keyPoints.Clear();

        ChangeColor();
    }

    private void ChangeColor()
    {
        var pixelData = _pixelsByPosition.FirstOrDefault(pixel => !pixel.Value.IsFinished).Value;

        if (pixelData == null)
        {
            Console.WriteLine("IMAGE FINISHED");
            return;
        }
        
        CurrentColor = pixelData.OriginalColor;
        
        _keyPoints.Clear();
        foreach (var pixel in _pixelsByPosition.Where(p => p.Value.OriginalColor == CurrentColor && !p.Value.IsFinished))
        {
            _keyPoints.Add(pixel.Key);
            pixel.Value.CurrentColor = _highlightColor;
        }
    }
    
    private void BuildKeyPoints()
    {
        _keyPoints.Clear();

        if (_contour.Count < 3)
        {
            return;
        }

        var points = _contour.ConvertAll(p => new Vector2(p.X, p.Y));
        var simplified = _contourService.SimplifyClosedContour(points, .75f);

        foreach (var point in simplified)
        {
            _keyPoints.Add(new Point(
                (int)MathF.Round(point.X),
                (int)MathF.Round(point.Y)
            ));
        }
    }
}