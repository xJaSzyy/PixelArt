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
    public Color CurrentColor { get; set; }
    public bool ContourFinished { get; private set; } = false;
    
    private readonly Color _highlightColor = new(72, 72, 72);
    
    private const int _gridWidth = 32;
    private const int _gridHeight = 32;
    private const int _pixelSize = 16;
    
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

        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);

        CurrentColor = Color.Yellow;
        
        Reset();
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        for (var y = 0; y < _gridHeight; y++)
        {
            for (var x = 0; x < _gridWidth; x++)
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
               x < _gridWidth &&
               y >= 0 &&
               y < _gridHeight;
    }

    public bool TryPaint(Vector2 screenPosition)
    {
        if (!TryGetGridPosition(screenPosition, out var x, out var y))
        {
            return false;
        }

        var pixel = _pixelsByPosition[new Point(x, y)];

        if (pixel == null)
        {
            return false;
        }

        if (ContourFinished && (pixel.OriginalColor == Color.Transparent || pixel.IsFinished))
        {
            return false;
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
            TryChangeColor();
        }
        
        return true;
    }

    public void Center(int viewportWidth, int viewportHeight)
    {
        var width = _gridWidth * _pixelSize;
        var height = _gridHeight * _pixelSize;

        var zoom = cameraService.Zoom;

        var viewportWorldWidth = viewportWidth / zoom;
        var viewportWorldHeight = viewportHeight / zoom;

        Position = new Vector2(
            (viewportWorldWidth - width) / 2f,
            (viewportWorldHeight - height) / 2f
        );
    }

    private bool IsSolid(int x, int y)
    {
        if (x < 0 || x >= _sourceTexture.Width ||
            y < 0 || y >= _sourceTexture.Height)
        {
            return false;
        }

        return _sourceData[y * _sourceTexture.Width + x].A > 0;
    }
    
    private bool IsBoundary(int x, int y)
    {
        if (!IsSolid(x, y))
        {
            return false;
        }

        return
            !IsSolid(x - 1, y) ||
            !IsSolid(x + 1, y) ||
            !IsSolid(x, y - 1) ||
            !IsSolid(x, y + 1);
    }

    public void Reset()
    {
        CurrentColor = Color.Yellow;
        
        _contour.Clear();
        _contour.AddRange(TraceContour());
        BuildKeyPoints();
        
        _pixelsByPosition.Clear();
        
        ContourFinished = false;
        
        _colorIndexes = new Dictionary<Color, int>();

        for (var y = 0; y < _gridHeight; y++)
        {
            for (var x = 0; x < _gridWidth; x++)
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
            if (IsPointNear(contourPoint, painted, 1))
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
        ContourFinished = true;
        
        foreach (var pixel in _pixelsByPosition)
        {
            var color = pixel.Value.GrayColor;
            
            if (_contour.Contains(pixel.Value.Position))
            {
                color = pixel.Value.OriginalColor;
            }

            pixel.Value.CurrentColor = color;
        }
        
        _keyPoints.Clear();

        TryChangeColor();
    }

    private bool TryChangeColor()
    {
        var pixelData = _pixelsByPosition.FirstOrDefault(pixel => !pixel.Value.IsFinished).Value;

        if (pixelData == null)
        {
            Console.WriteLine("IMAGE FINISHED");
            return false;
        }
        
        CurrentColor = pixelData.OriginalColor;
        
        _keyPoints.Clear();
        foreach (var pixel in _pixelsByPosition.Where(p => p.Value.OriginalColor == CurrentColor && !p.Value.IsFinished))
        {
            _keyPoints.Add(pixel.Key);
            pixel.Value.CurrentColor = _highlightColor;
        }

        return true;
    }

    private static bool IsPointNear(Point point, HashSet<Point> points, int tolerance)
    {
        for (var y = -tolerance; y <= tolerance; y++)
        {
            for (var x = -tolerance; x <= tolerance; x++)
            {
                if (points.Contains(new Point(
                        point.X + x,
                        point.Y + y)))
                {
                    return true;
                }
            }
        }

        return false;
    }
    
    private static float DistanceToLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        var line = lineEnd - lineStart;

        if (line.LengthSquared() == 0)
            return Vector2.Distance(point, lineStart);

        var cross = MathF.Abs(
            line.X * (lineStart.Y - point.Y) -
            (lineStart.X - point.X) * line.Y);

        return cross / line.Length();
    }
    
    private List<Point> TraceContour()
    {
        var contour = new List<Point>();

        Point start = new(-1, -1);

        for (var y = 0; y < _gridHeight && start.X == -1; y++)
        {
            for (var x = 0; x < _gridWidth; x++)
            {
                if (IsBoundary(x, y))
                {
                    start = new Point(x, y);
                    break;
                }
            }
        }

        if (start.X == -1)
            return contour;

        Point[] directions =
        [
            new(0, -1),
            new(1, -1),
            new(1, 0),
            new(1, 1),
            new(0, 1),
            new(-1, 1),
            new(-1, 0),
            new(-1, -1)
        ];

        var current = start;
        var previousDirection = 0;

        contour.Add(current);

        var maxIterations = _gridWidth * _gridHeight * 8;

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var found = false;

            var startDirection = (previousDirection + 6) % 8;

            for (var i = 0; i < 8; i++)
            {
                var directionIndex = (startDirection + i) % 8;
                var direction = directions[directionIndex];

                var next = new Point(
                    current.X + direction.X,
                    current.Y + direction.Y);

                if (!IsSolid(next.X, next.Y))
                {
                    continue;
                }

                current = next;
                previousDirection = directionIndex;

                if (current == start)
                {
                    if (contour.Count > 2)
                    {
                        return contour;
                    }
                }

                if (contour[^1] != current)
                {
                    contour.Add(current);
                }

                found = true;
                break;
            }

            if (!found)
            {
                break;
            }
        }

        return contour;
    }
    
    private void BuildKeyPoints()
    {
        _keyPoints.Clear();

        if (_contour.Count < 3)
        {
            return;
        }

        var points = _contour.ConvertAll(p => new Vector2(p.X, p.Y));
        var simplified = SimplifyClosedContour(points, 1f);

        foreach (var point in simplified)
        {
            _keyPoints.Add(new Point(
                (int)MathF.Round(point.X),
                (int)MathF.Round(point.Y)
            ));
        }
    }
    
    private static List<Vector2> SimplifyClosedContour(List<Vector2> points, float tolerance)
    {
        if (points.Count <= 3)
        {
            return [..points];
        }

        var first = points[0];

        var maxDistance = 0f;
        var splitIndex = 0;

        for (var i = 1; i < points.Count; i++)
        {
            var distance = Vector2.DistanceSquared(first, points[i]);

            if (distance > maxDistance)
            {
                maxDistance = distance;
                splitIndex = i;
            }
        }

        var part1 = new List<Vector2>();

        for (var i = 0; i <= splitIndex; i++)
        {
            part1.Add(points[i]);
        }

        var part2 = new List<Vector2>();

        for (var i = splitIndex; i < points.Count; i++)
        {
            part2.Add(points[i]);
        }

        var result1 = SimplifyOpenContour(part1, tolerance);
        var result2 = SimplifyOpenContour(part2, tolerance);

        result1.RemoveAt(result1.Count - 1);
        result2.RemoveAt(result2.Count - 1);
        result1.AddRange(result2);

        return result1;
    }
    
    private static List<Vector2> SimplifyOpenContour(List<Vector2> points, float tolerance)
    {
        if (points.Count <= 2)
        {
            return [..points];
        }

        var maxDistance = 0f;
        var index = 0;

        for (var i = 1; i < points.Count - 1; i++)
        {
            var distance = DistanceToLine(
                points[i],
                points[0],
                points[^1]);

            if (distance > maxDistance)
            {
                maxDistance = distance;
                index = i;
            }
        }

        if (maxDistance <= tolerance)
        {
            return
            [
                points[0],
                points[^1]
            ];
        }

        var left = points.GetRange(0, index + 1);
        var right = points.GetRange(index, points.Count - index);

        var leftResult = SimplifyOpenContour(left, tolerance);
        var rightResult = SimplifyOpenContour(right, tolerance);

        leftResult.RemoveAt(leftResult.Count - 1);
        leftResult.AddRange(rightResult);

        return leftResult;
    }
    
    public void ZoomAt(Vector2 mousePosition, float oldZoom, float newZoom)
    {
        var cameraPosition = cameraService.GetPosition();

        var oldCellSize = _pixelSize * oldZoom;
        var newCellSize = _pixelSize * newZoom;

        var mouseRelativeToImage = mousePosition - cameraPosition - Position;

        Position = mousePosition
                   - cameraPosition
                   - mouseRelativeToImage / oldCellSize * newCellSize;
    }
}