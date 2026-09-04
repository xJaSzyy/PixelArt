using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
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
    private const float _grayTransitionDuration = 0.8f;
    
    private float _grayTransitionTime;
    private bool _isFadingToGray;
    private Color _currentHighlightedColor;
    
    private ContourService _contourService;
    
    private Texture2D _sourceTexture;
    private Color[] _sourceData;
    private Texture2D _pixelTexture;
    private readonly List<Point> _contour = [];
    private readonly List<Point> _keyPoints = [];
    private Dictionary<Color, int> _colorIndexes;
    private readonly Dictionary<Point, Pixel> _pixelsByPosition = [];
    
    private Point? _startPoint;
    private Point? _endPoint;
    
    private Point? _lastPaintPoint;

    public void LoadContent(Texture2D texture)
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
    
    public void Update(GameTime gameTime)
    {
        CheckContourFinished();
        
        if (!_isFadingToGray)
        {
            return;
        }

        _grayTransitionTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_grayTransitionTime >= _grayTransitionDuration)
        {
            _grayTransitionTime = _grayTransitionDuration;
            _isFadingToGray = false;

            foreach (var pixel in _pixelsByPosition.Values.Where(pixel => !pixel.IsFinished))
            {
                pixel.CurrentColor = pixel.GrayColor;
            }
            
            ChangeColor();
        }
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
                
                var progress = MathHelper.Clamp(_grayTransitionTime / _grayTransitionDuration, 0f, 1f);
                
                var pixelColor = pixel.CurrentColor;
                
                if (_isFadingToGray && !pixel.IsFinished)
                {
                    pixelColor = Color.Lerp(pixel.CurrentColor, pixel.OriginalColor == _currentHighlightedColor ? _highlightColor : pixel.GrayColor, progress);
                }
                
                spriteBatch.Draw(_pixelTexture, rectangle, pixelColor);

                if ((_keyPoints.Contains(pixel.Position) || pixel.CurrentColor == _highlightColor) && !pixel.IsFinished)
                {
                    var text = (_keyPoints.LastIndexOf(pixel.Position) + 1).ToString();
                    var color = pixel.CurrentColor == Color.Yellow ? Color.Black : Color.Yellow;
                    
                    if (ContourFinished)
                    {
                        text = (_colorIndexes[CurrentColor] + 1).ToString();
                        color = Color.Lerp(
                            Color.Transparent,
                            Colors.IsDark(pixel.CurrentColor) ? Color.White : Color.Black,
                            Utils.Remap(cameraService.Zoom, cameraService.MinZoom, cameraService.MinZoom * 1.5f, 0f, 1f) * progress
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
            _lastPaintPoint = null;
            return;
        }

        var currentPoint = new Point(x, y);

        if (_lastPaintPoint == null)
        {
            PaintPixel(currentPoint);
            _lastPaintPoint = currentPoint;
            return;
        }

        foreach (var point in GetLine(_lastPaintPoint.Value, currentPoint))
        {
            PaintPixel(point);
        }

        _lastPaintPoint = currentPoint;
    }
    
    private void PaintPixel(Point point)
    {
        if (!_pixelsByPosition.TryGetValue(point, out var pixel))
        {
            return;
        }

        if (pixel == null)
        {
            return;
        }

        if (ContourFinished &&
            (pixel.OriginalColor == Color.Transparent || pixel.IsFinished))
        {
            return;
        }

        var currentColor = CurrentColor;

        if (ContourFinished && pixel.OriginalColor != currentColor)
        {
            currentColor = Color.Lerp(CurrentColor, pixel.GrayColor, 0.6f);
        }

        if (!ContourFinished)
        {
            CheckKeyPointsConnected(pixel.Position);

            if (pixel.CurrentColor == pixel.OriginalColor &&
                pixel.CurrentColor != Color.Transparent)
            {
                return;
            }
        }

        pixel.CurrentColor = currentColor;

        if (ContourFinished &&
            _pixelsByPosition
                .Where(p => p.Value.OriginalColor == CurrentColor)
                .All(p => p.Value.IsFinished))
        {
            ChangeColor();
        }
    }
    
    private static IEnumerable<Point> GetLine(Point from, Point to)
    {
        var x0 = from.X;
        var y0 = from.Y;

        var x1 = to.X;
        var y1 = to.Y;

        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);

        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;

        var error = dx - dy;

        while (true)
        {
            yield return new Point(x0, y0);

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var e2 = error * 2;

            if (e2 > -dy)
            {
                error -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }
    
    public void StopPainting()
    {
        _lastPaintPoint = null;
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

        _startPoint = null;
        _endPoint = null;
        
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

    private void CheckContourFinished()
    {
        if (_contour.Count == 0 || ContourFinished)
        {
            return;
        }
        
        foreach (var point in _contour)
        {
            if (!_pixelsByPosition[point].IsFinished)
            {
                return;
            }
        }
        
        FinishContour();
    }

    private void FinishContour()
    {
        ContourFinished = true;
        _keyPoints.Clear();

        ChangeColor();
        
        _grayTransitionTime = 0f;
        _isFadingToGray = true;
    }

    private void ChangeColor()
    {
        var pixelData = _pixelsByPosition
            .Where(pixel => !pixel.Value.IsFinished && _colorIndexes.ContainsKey(pixel.Value.OriginalColor))
            .OrderBy(pixel => _colorIndexes[pixel.Value.OriginalColor])
            .FirstOrDefault().Value;

        if (pixelData == null)
        {
            Console.WriteLine("IMAGE FINISHED");
            return;
        }
        
        CurrentColor = pixelData.OriginalColor;
        _currentHighlightedColor = pixelData.OriginalColor;
        
        _keyPoints.Clear();
        foreach (var pixel in _pixelsByPosition
                     .Where(p => p.Value.OriginalColor == CurrentColor && !p.Value.IsFinished))
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

    private void CheckKeyPointsConnected(Point point)
    {
        if (_keyPoints.Contains(point))
        {
            if (_startPoint == null)
            {
                _startPoint = point;
            }
            else if (_endPoint == null && point != _startPoint)
            {
                _endPoint = point;
            }
        }

        if (_startPoint == null || _endPoint == null)
        {
            return;
        }

        if (Math.Abs(_keyPoints.IndexOf(_startPoint.Value) - _keyPoints.LastIndexOf(_endPoint.Value)) != 1)
        {
            _startPoint = _endPoint;
            _endPoint = null;
            return;
        }

        var startIndex = _contour.IndexOf(_startPoint.Value);
        var endIndex = _contour.LastIndexOf(_endPoint.Value);

        if (_contour.IndexOf(_startPoint.Value) > _contour.LastIndexOf(_endPoint.Value))
        {
            startIndex = _contour.LastIndexOf(_endPoint.Value);
            endIndex = _contour.IndexOf(_startPoint.Value);
        }

        var pixelsCount = 0;
        var paintedPixelsCount = 0;

        for (var i = startIndex; i < endIndex; i++)
        {
            pixelsCount++;

            if (_pixelsByPosition[_contour[i]].CurrentColor != Color.Transparent)
            {
                paintedPixelsCount++;
            }
        }

        if (!((float)paintedPixelsCount / pixelsCount > .8f))
        {
            return;
        }

        for (var i = startIndex; i <= endIndex; i++)
        {
            var contourPoint = _contour[i];

            _pixelsByPosition[contourPoint].CurrentColor = _pixelsByPosition[contourPoint].OriginalColor;

            if (contourPoint == _keyPoints[0] && _keyPoints[0] != _keyPoints[^1])
            {
                var firstPoint = _keyPoints[0];
                _keyPoints.Add(firstPoint);
                _contour.Add(firstPoint);
                _pixelsByPosition[firstPoint].CurrentColor = Color.Transparent;
            }
        }

        foreach (var pixel in _pixelsByPosition.Values.Where(pixels => !pixels.IsFinished))
        {
            pixel.CurrentColor = Color.Transparent;
        }

        _startPoint = null;
        _endPoint = null;
    }
}