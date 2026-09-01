using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Models;

namespace PixelArt.Services;

public class PixelService(GraphicsDevice graphicsDevice, DrawService drawService)
{
    private Vector2 Position { get; set; }
    public Color CurrentColor { get; set; } = Color.YellowGreen;
    
    private const int _gridWidth = 32;
    private const int _gridHeight = 32;
    private const int _pixelSize = 16;
    
    private readonly List<Pixel> _pixels = [];
    private Texture2D _sourceTexture;
    private Color[] _sourceData;
    private Texture2D _pixelTexture;
    private readonly List<Point> _contour = [];
    private readonly List<Point> _keyPoints = [];

    private Rectangle Bounds => new(
        (int)Position.X,
        (int)Position.Y,
        _gridWidth * _pixelSize,
        _gridHeight * _pixelSize
    );

    public void LoadContent(ContentManager content)
    {
        _sourceTexture = content.Load<Texture2D>("Images/img20");
        _sourceData = new Color[_sourceTexture.Width * _sourceTexture.Height];
        _sourceTexture.GetData(_sourceData);

        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);

        _contour.Clear();
        _contour.AddRange(TraceContour());
        BuildKeyPoints();
        
        _pixels.Clear();
        Reset();
        
        foreach (var point in _keyPoints)
        {
            var pixel = _pixels.FirstOrDefault(x => x.Position == point);

            if (pixel != null)
            {
                pixel.CurrentColor = Color.DarkRed;
            }
        }
    }

    public bool CheckContourMatch(float requiredPercent = 0.98f)
    {
        if (_contour.Count == 0)
        {
            return false;
        }

        var painted = _pixels
            .Where(x => x.CurrentColor.A > 0)
            .Select(x => x.Position)
            .ToHashSet();

        if (painted.Count == 0)
        {
            return false;
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

        Console.WriteLine($"Contour: {contourCoverage:P1}, " + $"Result: {result}");

        if (result)
        {
            _pixels.ForEach(pixel => pixel.CurrentColor = pixel.GrayColor);
        }

        return result;
    }
    
    private static bool IsPointNear(
        Point point,
        HashSet<Point> points,
        int tolerance)
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
    
    private static bool IsPointNear(
        Point point,
        List<Point> points,
        int tolerance)
    {
        for (var y = -tolerance; y <= tolerance; y++)
        {
            for (var x = -tolerance; x <= tolerance; x++)
            {
                var target = new Point(
                    point.X + x,
                    point.Y + y);

                if (points.Contains(target))
                    return true;
            }
        }

        return false;
    }
    
    private static float DistanceToLine(
        Vector2 point,
        Vector2 lineStart,
        Vector2 lineEnd)
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
    
    public void Reset()
    {
        var colorIndexes = new Dictionary<Color, int>();
        
        for (var y = 0; y < _gridHeight; y++)
        {
            for (var x = 0; x < _gridWidth; x++)
            {
                var index = y * _sourceTexture.Width + x;
                var originalColor = _sourceData[index];

                if (originalColor.A == 255 && !colorIndexes.ContainsKey(originalColor))
                {
                    colorIndexes[originalColor] = colorIndexes.Count;
                }

                var colorIndex = colorIndexes.GetValueOrDefault(originalColor, -1);

                _pixels.Add(new Pixel
                {
                    X = x,
                    Y = y,
                    CurrentColor = Color.Transparent,
                    OriginalColor = _sourceData[index],
                    GrayColor = colorIndex >= 0
                        ? Utils.GenerateGrayColor(colorIndex, colorIndexes.Count)
                        : Color.Transparent
                });
            }
        }
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
    
    private void BuildKeyPoints()
    {
        _keyPoints.Clear();

        if (_contour.Count < 3)
        {
            return;
        }

        var points = _contour.ConvertAll(p => new Vector2(p.X, p.Y));
        var simplified = SimplifyClosedContour(points, 1.5f);

        foreach (var point in simplified)
        {
            _keyPoints.Add(new Point(
                (int)MathF.Round(point.X),
                (int)MathF.Round(point.Y)
            ));
        }
    }
    
    private List<Vector2> SimplifyClosedContour(List<Vector2> points, float tolerance)
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
    
    private List<Vector2> SimplifyOpenContour(List<Vector2> points, float tolerance)
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
    
    public void Center(int viewportWidth, int viewportHeight)
    {
        Position = new Vector2(
            (viewportWidth - _gridWidth * _pixelSize) / 2f,
            (viewportHeight - _gridHeight * _pixelSize) / 2f
        );
    }

    private bool TryGetGridPosition(Vector2 screenPosition, out int x, out int y)
    {
        x = -1;
        y = -1;

        if (!Bounds.Contains(screenPosition))
        {
            return false;
        }

        var localPosition = screenPosition - Position;

        x = (int)(localPosition.X / _pixelSize);
        y = (int)(localPosition.Y / _pixelSize);

        return x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight;
    }

    public bool TryPaint(Vector2 screenPosition)
    {
        if (!TryGetGridPosition(screenPosition, out var x, out var y))
        {
            return false;
        }

        var pixel = _pixels.FirstOrDefault(pixel => pixel.Position == new Point(x, y));

        if (pixel == null)
        {
            return false;
        }

        pixel.CurrentColor = CurrentColor;

        return true;
    }

    public bool TryErase(Vector2 screenPosition)
    {
        if (!TryGetGridPosition(screenPosition, out var x, out var y))
        {
            return false;
        }

        var pixel = _pixels.FirstOrDefault(pixel => pixel.Position == new Point(x, y));

        if (pixel == null)
        {
            return false;
        }

        pixel.CurrentColor = Color.Transparent;


        return true;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        for (var y = 0; y < _gridHeight; y++)
        {
            for (var x = 0; x < _gridWidth; x++)
            {
                var rectangle = new Rectangle(
                    (int)(Position.X + x * _pixelSize),
                    (int)(Position.Y + y * _pixelSize),
                    _pixelSize,
                    _pixelSize
                );

                var pixel = _pixels.FirstOrDefault(pixel => pixel.Position == new Point(x, y));

                if (pixel == null)
                {
                    continue;
                }

                spriteBatch.Draw(_pixelTexture, rectangle, pixel.CurrentColor);

                if (_keyPoints.Contains(pixel.Position))
                {
                    drawService.DrawString(spriteBatch, (_keyPoints.IndexOf(pixel.Position) + 1).ToString(), rectangle.Center.ToVector2(), Color.White, .75f);
                }
            }
        }
    }
}