using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt.Services;

public class PixelService
{
    public const int GridWidth = 32;
    public const int GridHeight = 32;

    private readonly GraphicsDevice _graphicsDevice;

    private Texture2D _sourceTexture;
    private Color[] _sourceData;

    private Texture2D _pixelTexture;

    private readonly Color[,] _colors = new Color[GridWidth, GridHeight];

    private readonly bool[,] _painted = new bool[GridWidth, GridHeight];

    public float PixelSize { get; set; } = 24f;

    public Vector2 Position { get; private set; }

    public Color CurrentColor { get; set; } = Color.YellowGreen;
    
    private readonly List<Point> _contour = [];
    private readonly List<Point> _keyPoints = [];

    private Rectangle Bounds => new(
        (int)Position.X,
        (int)Position.Y,
        GridWidth * (int)PixelSize,
        GridHeight * (int)PixelSize
    );

    public PixelService(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _sourceTexture = content.Load<Texture2D>("Images/img20");
        _sourceData = new Color[_sourceTexture.Width * _sourceTexture.Height];
        _sourceTexture.GetData(_sourceData);

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);

        _contour.Clear();
        _contour.AddRange(TraceContour());
        BuildKeyPoints();
        
        Reset();
        
        foreach (var point in _contour)
        {
            _colors[point.X, point.Y] = Color.DarkBlue;
            _painted[point.X, point.Y] = true;
        }
        
        foreach (var point in _keyPoints)
        {
            _colors[point.X, point.Y] = Color.DarkRed;
            _painted[point.X, point.Y] = true;
        }
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

        for (var y = 0; y < GridHeight && start.X == -1; y++)
        {
            for (var x = 0; x < GridWidth; x++)
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
            new Point(0, -1),
            new Point(1, -1),
            new Point(1, 0),
            new Point(1, 1),
            new Point(0, 1),
            new Point(-1, 1),
            new Point(-1, 0),
            new Point(-1, -1)
        ];

        var current = start;
        var previousDirection = 0;

        contour.Add(current);

        var maxIterations = GridWidth * GridHeight * 8;

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
                    continue;

                current = next;
                previousDirection = directionIndex;

                if (current == start)
                {
                    if (contour.Count > 2)
                        return contour;
                }

                if (contour[^1] != current)
                    contour.Add(current);

                found = true;
                break;
            }

            if (!found)
                break;
        }

        return contour;
    }
    
    public void Reset()
    {
        for (var y = 0; y < GridHeight; y++)
        {
            for (var x = 0; x < GridWidth; x++)
            {
                _colors[x, y] = Color.Transparent;
                _painted[x, y] = false;
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
            return false;

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
            return;

        var points = _contour
            .ConvertAll(p => new Vector2(p.X, p.Y));

        var simplified = SimplifyClosedContour(points, 1.5f);

        foreach (var point in simplified)
        {
            _keyPoints.Add(new Point(
                (int)MathF.Round(point.X),
                (int)MathF.Round(point.Y)
            ));
        }
    }
    
    private List<Vector2> SimplifyClosedContour(
        List<Vector2> points,
        float tolerance)
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
    
    private List<Vector2> SimplifyOpenContour(
        List<Vector2> points,
        float tolerance)
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
        var width = GridWidth * PixelSize;
        var height = GridHeight * PixelSize;

        Position = new Vector2(
            (viewportWidth - width) / 2f,
            (viewportHeight - height) / 2f
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

        x = (int)(localPosition.X / PixelSize);
        y = (int)(localPosition.Y / PixelSize);

        return x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
    }

    public bool TryPaint(Vector2 screenPosition)
    {
        if (!TryGetGridPosition(screenPosition, out var x, out var y))
        {
            return false;
        }

        _colors[x, y] = CurrentColor;
        _painted[x, y] = true;

        return true;
    }

    public bool TryErase(Vector2 screenPosition)
    {
        if (!TryGetGridPosition(screenPosition, out var x, out var y))
        {
            return false;
        }

        _colors[x, y] = Color.Transparent;
        _painted[x, y] = false;

        return true;
    }

    public Color GetPixelColor(int x, int y)
    {
        if (x < 0 || x >= GridWidth ||
            y < 0 || y >= GridHeight)
        {
            return Color.Transparent;
        }

        return _colors[x, y];
    }

    public void SetPixelColor(int x, int y, Color color)
    {
        if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
        {
            return;
        }

        _colors[x, y] = color;
        _painted[x, y] = true;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        for (var y = 0; y < GridHeight; y++)
        {
            for (var x = 0; x < GridWidth; x++)
            {
                var rectangle = new Rectangle(
                    (int)(Position.X + x * PixelSize),
                    (int)(Position.Y + y * PixelSize),
                    (int)PixelSize,
                    (int)PixelSize
                );

                if (_painted[x, y])
                {
                    spriteBatch.Draw(_pixelTexture, rectangle, _colors[x, y]);
                }
                else
                {
                    var sourceColor = _sourceData[y * _sourceTexture.Width + x];

                    if (sourceColor.A == 0)
                    {
                        spriteBatch.Draw(
                            _pixelTexture,
                            rectangle,
                            new Color(80, 80, 80)
                        );
                    }
                    else
                    {
                        spriteBatch.Draw(
                            _pixelTexture,
                            rectangle,
                            new Rectangle(x, y, 1, 1),
                            new Color(72, 72, 72)
                        );
                    }
                }
            }
        }
    }
}