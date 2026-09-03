using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt.Services;

public class ContourService
{
    private readonly Texture2D _sourceTexture;
    private readonly Color[] _sourceData;
    private readonly Point _gridSize;

    public ContourService(Texture2D sourceTexture, Color[] sourceData, Point gridSize)
    {
        _sourceTexture = sourceTexture;
        _sourceData = sourceData;
        _gridSize = gridSize;
    }

    public List<Point> TraceContour()
    {
        var contour = new List<Point>();

        Point start = new(-1, -1);

        for (var y = 0; y < _gridSize.Y && start.X == -1; y++)
        {
            for (var x = 0; x < _gridSize.X; x++)
            {
                if (IsBoundary(x, y))
                {
                    start = new Point(x, y);
                    break;
                }
            }
        }

        if (start.X == -1)
        {
            return contour;
        }

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

        var maxIterations = _gridSize.X * _gridSize.Y * 8;

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

    public List<Vector2> SimplifyClosedContour(List<Vector2> points, float tolerance)
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
            var distance = Utils.DistanceToLine(points[i], points[0], points[^1]);

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
    
    private bool IsSolid(int x, int y)
    {
        if (x < 0 || x >= _sourceTexture.Width || y < 0 || y >= _sourceTexture.Height)
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

        return !IsSolid(x - 1, y) || !IsSolid(x + 1, y) || 
               !IsSolid(x, y - 1) || !IsSolid(x, y + 1);
    }
}