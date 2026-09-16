using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Enums;
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
    private Color[] _texturePixels;
    
    private Color[][] _cubeTexturePixels;
    private PixelData[][] _cubePixelLookup;
    //private readonly Dictionary<int, Point?> _lastPaintByFace = new();  
    private readonly Dictionary<CubeFace, Point> _lastPaintByFace = new();
    
    private PixelData[] _pixelLookup;
    private readonly Color _highlightColor = new(72, 72, 72);
    private readonly HashSet<int> _highlightedPixels = [];
    private bool _textureDirty;
    private readonly Dictionary<Color, PixelColorGroup> _groupsByColor = [];

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ParticleService _particleService;
    private readonly CameraService _cameraService;
    private readonly SoundService _soundService;

    private float _minNumberPixelSize = 14f;
    private const float _replayDuration = 1.25f;
    
    private readonly Color _glowColor = new(171, 171, 171, 200);

    private TexturedCube cube;
    private float _rotationX;
    private float _rotationY;

    private bool _isDragging;
    private Point _previousMousePosition;

    private const float _rotationSpeed = 0.01f;
    
    public PixelProcessorService(GraphicsDevice graphicsDevice, ParticleService particleService, CameraService cameraService, SoundService soundService)
    {
        _graphicsDevice = graphicsDevice;
        _particleService = particleService;
        _cameraService = cameraService;
        _soundService = soundService;
    }

    public void SetLevel(LevelData levelData)
    {
        CurrentLevel = levelData;
        
        _minNumberPixelSize = CurrentLevel.Texture.Width / 6f;
        
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
            _groupsByColor[group.OriginalColor.ToColor()] = group;
        }

        _highlightedPixels.Clear();
        _lastPaintPixel = null;
        _textureDirty = false;
        
        if (CurrentLevel.Type == LevelType.ThreeD)
        {
            cube = new TexturedCube(
                _graphicsDevice,
                CurrentLevel.CubeTextures[0],
                CurrentLevel.CubeTextures[1],
                CurrentLevel.CubeTextures[2],
                CurrentLevel.CubeTextures[3],
                CurrentLevel.CubeTextures[4],
                CurrentLevel.CubeTextures[5]);
        }
        else
        {
            cube = new TexturedCube(
                _graphicsDevice,
                CurrentLevel.Texture,
                CurrentLevel.Texture,
                CurrentLevel.Texture,
                CurrentLevel.Texture,
                CurrentLevel.Texture,
                CurrentLevel.Texture);
        }
        
        projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(45f),
            _graphicsDevice.Viewport.AspectRatio,
            0.1f,
            100f);

        view = Matrix.CreateLookAt(
            new Vector3(2f, 2f, 4f),
            Vector3.Zero,
            Vector3.Up);
        
        world = Matrix.Identity;
        _rotationX = 0f;
        _rotationY = 0f;
        
        if (CurrentLevel.Type == LevelType.ThreeD)
        {
            InitializeCubeData();
        }
    }
    
    private void InitializeCubeData()
    {
        const int faceCount = 6;

        _cubeTexturePixels = new Color[faceCount][];
        _cubePixelLookup = new PixelData[faceCount][];

        for (var face = 0; face < faceCount; face++)
        {
            var texture = CurrentLevel.CubeTextures[face];

            var size = texture.Width * texture.Height;

            _cubeTexturePixels[face] = new Color[size];
            _cubePixelLookup[face] = new PixelData[size];

            texture.GetData(_cubeTexturePixels[face]);
        }
    }
    
    public void Process3DImage()
    {
        if (CurrentLevel.Type != LevelType.ThreeD)
        {
            return;
        }

        if (CurrentLevel.Pixels.Count == 0)
        {
            ProcessNew3DImage();
        }
        else
        {
            RebuildExisting3DImage();
        }

        _textureDirty = true;
    }
    
    private void ProcessNew3DImage()
    {
        CurrentLevel.ColorGroups.Clear();
        CurrentLevel.Pixels.Clear();

        _groupsByColor.Clear();

        for (var face = 0; face < 6; face++)
        {
            var texture = CurrentLevel.CubeTextures[face];
            var pixels = _cubeTexturePixels[face];

            for (var i = 0; i < pixels.Length; i++)
            {
                var original = pixels[i];

                if (original.A != 255)
                {
                    continue;
                }

                if (!_groupsByColor.TryGetValue(original, out var group))
                {
                    group = new PixelColorGroup
                    {
                        OriginalColor = new ColorData(original),
                        Pixels = []
                    };

                    _groupsByColor.Add(original, group);
                    CurrentLevel.ColorGroups.Add(group);
                }

                var pixel = new PixelData
                {
                    Index = i,
                    Face = face,
                    OriginalColor = new ColorData(original),
                    CurrentColor = new ColorData(Color.White)
                };

                CurrentLevel.Pixels.Add(pixel);

                _cubePixelLookup[face][i] = pixel;

                group.Pixels.Add(pixel);
            }
        }

        var total = CurrentLevel.ColorGroups.Count;

        SortAndNumberColorGroups();

        for (var i = 0; i < total; i++)
        {
            var group = CurrentLevel.ColorGroups[i];

            var grayColor = Utils.GenerateGrayColor(i, total);

            foreach (var pixel in group.Pixels)
            {
                pixel.CurrentColor = new ColorData(grayColor);
                pixel.GrayColor = new ColorData(grayColor);

                _cubeTexturePixels[pixel.Face][pixel.Index] = grayColor;
            }
        }

        for (var face = 0; face < 6; face++)
        {
            CurrentLevel.CubeTextures[face].SetData(
                _cubeTexturePixels[face]);
        }
    }
    
    private void RebuildExisting3DImage()
    {
        _groupsByColor.Clear();

        for (var face = 0; face < 6; face++)
        {
            Array.Clear(_cubePixelLookup[face], 0, _cubePixelLookup[face].Length);

            var pixels = _cubeTexturePixels[face];

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.Transparent;
            }
        }

        CurrentLevel.ColorGroups.Clear();

        foreach (var pixel in CurrentLevel.Pixels)
        {
            _cubePixelLookup[pixel.Face][pixel.Index] = pixel;

            _cubeTexturePixels[pixel.Face][pixel.Index] =
                pixel.CurrentColor.ToColor();

            if (!_groupsByColor.TryGetValue(
                    pixel.OriginalColor.ToColor(),
                    out var group))
            {
                group = new PixelColorGroup
                {
                    OriginalColor = pixel.OriginalColor,
                    Pixels = []
                };

                _groupsByColor.Add(
                    pixel.OriginalColor.ToColor(),
                    group);

                CurrentLevel.ColorGroups.Add(group);
            }

            group.Pixels.Add(pixel);
        }

        SortAndNumberColorGroups();

        for (var face = 0; face < 6; face++)
        {
            CurrentLevel.CubeTextures[face].SetData(
                _cubeTexturePixels[face]);
        }
    }
    
    private Matrix world;
    private Matrix view;
    private Matrix projection;
    private float rotation;

    public void ProcessImage()
    {
        if (CurrentLevel.Type == LevelType.ThreeD)
        {
            Process3DImage();
            return;
        }

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

            if (_texturePixels[i].A != 255)
            {
                continue;
            }

            if (!_groupsByColor.TryGetValue(original, out var group))
            {
                group = new PixelColorGroup
                {
                    OriginalColor = new ColorData(original),
                    Pixels = []
                };

                _groupsByColor.Add(original, group);
                CurrentLevel.ColorGroups.Add(group);
            }

            var pixel = new PixelData
            {
                Index = i,
                OriginalColor = new ColorData(original),
                CurrentColor = new ColorData(Color.White)
            };

            CurrentLevel.Pixels.Add(pixel);

            _pixelLookup[i] = pixel;

            group.Pixels.Add(pixel);
        }

        var total = CurrentLevel.ColorGroups.Count;
        
        SortAndNumberColorGroups();

        for (var i = 0; i < total; i++)
        {
            var group = CurrentLevel.ColorGroups[i];

            var grayColor = Utils.GenerateGrayColor(i, total);

            foreach (var pixel in group.Pixels)
            {
                pixel.CurrentColor = new ColorData(grayColor);
                pixel.GrayColor = new ColorData(grayColor);

                _texturePixels[pixel.Index] = grayColor;
            }
        }
        
        CurrentLevel.GrayTexture.SetData(_texturePixels);
    }

    private void RebuildExistingImage()
    {
        _groupsByColor.Clear();

        foreach (var pixel in CurrentLevel.Pixels)
        {
            _pixelLookup[pixel.Index] = pixel;
            _texturePixels[pixel.Index] = pixel.CurrentColor.ToColor();

            if (!_groupsByColor.TryGetValue(pixel.OriginalColor.ToColor(), out var group))
            {
                group = new PixelColorGroup
                {
                    OriginalColor = pixel.OriginalColor,
                    Pixels = []
                };

                _groupsByColor.Add(pixel.OriginalColor.ToColor(), group);
                CurrentLevel.ColorGroups.Add(group);
            }
            
            group.Pixels.Add(pixel);
        }
        
        SortAndNumberColorGroups();

        var size = CurrentLevel.Texture.Width * CurrentLevel.Texture.Height;
        var texturePixels = new Color[size];
        
        foreach (var pixel in CurrentLevel.Pixels)
        {
            texturePixels[pixel.Index] = pixel.GrayColor.ToColor();
        }
        
        CurrentLevel.GrayTexture.SetData(texturePixels);
    }
    
    private void SortAndNumberColorGroups()
    {
        CurrentLevel.ColorGroups = CurrentLevel.ColorGroups
            .OrderByDescending(x =>
                0.2126 * x.OriginalColor.R +
                0.7152 * x.OriginalColor.G +
                0.0722 * x.OriginalColor.B).ToList();
        
        for (var i = 0; i < CurrentLevel.ColorGroups.Count; i++)
        {
            CurrentLevel.ColorGroups[i].Number = CurrentLevel.ColorGroups.Count - i;
        }
    }

    private bool TryGetPixelFromCube(MouseState mouse, out int pixelIndex, out Point pixelCoord)
    {
        pixelIndex = -1;
        pixelCoord = default;

        if (!TryGetCubeIntersection(mouse.Position, out var localPoint, out _))
        {
            return false;
        }

        var face = GetCubeFace(localPoint);
        var uv = GetFaceUv(face, localPoint);

        var width = CurrentLevel.Texture.Width;
        var height = CurrentLevel.Texture.Height;

        var x = Math.Clamp((int)(uv.X * width), 0, width - 1);
        var y = Math.Clamp((int)(uv.Y * height), 0, height - 1);

        pixelCoord = new Point(x, y);
        pixelIndex = y * width + x;

        return true;
    }
    
    public void PaintAtCube(MouseState mouse, Color color)
    {
        if (CurrentLevel.Type != LevelType.ThreeD)
        {
            return;
        }

        if (!TryGetCubeIntersection(
                mouse.Position,
                out var localPoint,
                out _))
        {
            _lastPaintByFace.Clear();
            return;
        }

        var face = GetCubeFace(localPoint);
        var uv = GetFaceUv(face, localPoint);

        var width = CurrentLevel.CubeTextures[(int)face].Width;
        var height = CurrentLevel.CubeTextures[(int)face].Height;

        var x = Math.Clamp(
            (int)(uv.X * width),
            0,
            width - 1);

        var y = Math.Clamp(
            (int)(uv.Y * height),
            0,
            height - 1);

        var current = new Point(x, y);

        if (_lastPaintByFace.TryGetValue(face, out var last))
        {
            foreach (var point in Utils.GetLine(last, current))
            {
                PaintBrush3D(face, point, color);
            }
        }
        else
        {
            PaintBrush3D(face, current, color);
        }

        _lastPaintByFace[face] = current;

        _textureDirty = true;
    }
    
    private void PaintBrush3D(
        CubeFace face,
        Point center,
        Color color)
    {
        var faceIndex = (int)face;

        var texture = CurrentLevel.CubeTextures[faceIndex];

        var width = texture.Width;
        var height = texture.Height;

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

                if (x < 0 || x >= width ||
                    y < 0 || y >= height)
                {
                    continue;
                }

                var index = y * width + x;

                SetPixel3D(faceIndex, index, color);
            }
        }
    }
    
    private void SetPixel3D(
        int face,
        int index,
        Color color)
    {
        if (face < 0 || face >= 6)
        {
            return;
        }

        var lookup = _cubePixelLookup[face];

        if (index < 0 || index >= lookup.Length)
        {
            return;
        }

        var pixel = lookup[index];

        if (pixel == null || pixel.IsFinished)
        {
            return;
        }

        if (color == pixel.OriginalColor.ToColor())
        {
            CurrentLevel.History.Add(
                GetHistoryIndex(face, index));

            var brightnessOffset =
                Colors.IsDark(color) ? 40 : -40;

            var particleColor = new Color(
                Math.Clamp(color.R + brightnessOffset, 0, 255),
                Math.Clamp(color.G + brightnessOffset, 0, 255),
                Math.Clamp(color.B + brightnessOffset, 0, 255));

            var localPosition = GetPixelWorldPosition(
                face,
                pixel);

            /*_particleService.Spawn(
                localPosition,
                particleColor,
                5);*/

            _soundService.PlayPaintingSound();

            _highlightedPixels.Remove(
                GetHistoryIndex(face, index));
        }
        else
        {
            color = Color.Lerp(
                color,
                pixel.GrayColor.ToColor(),
                0.6f);
        }

        pixel.CurrentColor = new ColorData(color);

        _cubeTexturePixels[face][index] = color;

        _textureDirty = true;
    }
    
    private const int HistoryFaceMultiplier = 1_000_000;

    private static int GetHistoryIndex(int face, int pixelIndex)
    {
        return face * HistoryFaceMultiplier + pixelIndex;
    }

    private static void DecodeHistoryIndex(
        int historyIndex,
        out int face,
        out int pixelIndex)
    {
        face = historyIndex / HistoryFaceMultiplier;
        pixelIndex = historyIndex % HistoryFaceMultiplier;
    }
    
    private float _cameraDistance = 5f;
    private readonly Vector3 _cameraDirection = Vector3.Normalize(new Vector3(2f, 2f, 4f));

    private void UpdateView()
    {
        view = Matrix.CreateLookAt(
            _cameraDirection * _cameraDistance,
            Vector3.Zero,
            Vector3.Up);
    }

    public void Zoom(float delta)
    {
        const float step = .05f;
        _cameraDistance = MathHelper.Clamp(_cameraDistance - (delta > 0 ? step : -step), 1f, 2.5f);
        UpdateView();
    }
    
    public void Update(GameTime gameTime, MouseState mouse)
    {
        _particleService.Update(gameTime);

        UpdateCubeRotation(mouse);
        
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

        while (_pixelsAccumulator >= 1f &&
               _historyIndex < historyCount)
        {
            var historyValue =
                CurrentLevel.History[_historyIndex++];

            if (CurrentLevel.Type == LevelType.ThreeD)
            {
                DecodeHistoryIndex(
                    historyValue,
                    out var face,
                    out var pixelIndex);

                if (face >= 0 && face < 6 &&
                    pixelIndex >= 0 &&
                    pixelIndex < _cubePixelLookup[face].Length)
                {
                    var pixel =
                        _cubePixelLookup[face][pixelIndex];

                    if (pixel != null)
                    {
                        pixel.CurrentColor =
                            pixel.OriginalColor;

                        _cubeTexturePixels[face][pixelIndex] =
                            pixel.OriginalColor.ToColor();

                        changed = true;
                    }
                }
            }
            else
            {
                var pixelIndex = historyValue;

                if (pixelIndex >= 0 &&
                    pixelIndex < _pixelLookup.Length)
                {
                    var pixel = _pixelLookup[pixelIndex];

                    if (pixel != null)
                    {
                        pixel.CurrentColor =
                            pixel.OriginalColor;

                        _texturePixels[pixelIndex] =
                            pixel.OriginalColor.ToColor();

                        changed = true;
                    }
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

    public void UpdateCubeRotation(MouseState mouse)
    {
        if (mouse.RightButton == ButtonState.Pressed)
        {
            if (!_isDragging)
            {
                _isDragging = true;
                _previousMousePosition = mouse.Position;
                return;
            }

            var delta = mouse.Position - _previousMousePosition;

            if (delta != Point.Zero)
            {
                // Горизонталь — вокруг экранной вертикали (мировой Y)
                // Вертикаль — вокруг экранной горизонтали (оси X камеры)
                var yaw = Matrix.CreateRotationY(delta.X * _rotationSpeed);
                var pitch = Matrix.CreateRotationX(delta.Y * _rotationSpeed);

                // Применяем к текущей матрице мира: сначала yaw, потом pitch в экранных осях
                world = world * yaw * pitch;
            }

            _previousMousePosition = mouse.Position;
        }
        else
        {
            _isDragging = false;
        }
    }
    
    private Ray CreateMouseRay(Point mousePosition)
    {
        var viewport = _graphicsDevice.Viewport;

        var nearSource = new Vector3(
            mousePosition.X,
            mousePosition.Y,
            0f);

        var farSource = new Vector3(
            mousePosition.X,
            mousePosition.Y,
            1f);

        var nearPoint = viewport.Unproject(
            nearSource,
            projection,
            view,
            Matrix.Identity);

        var farPoint = viewport.Unproject(
            farSource,
            projection,
            view,
            Matrix.Identity);

        var direction = farPoint - nearPoint;
        direction.Normalize();

        return new Ray(nearPoint, direction);
    }
    
    private static readonly BoundingBox LocalCubeBounds =
        new BoundingBox(
            new Vector3(-0.5f),
            new Vector3(0.5f));
    
    private bool TryGetCubeIntersection(
        Point mousePosition,
        out Vector3 localPoint,
        out float distance)
    {
        localPoint = default;
        distance = 0f;

        var ray = CreateMouseRay(mousePosition);

        Matrix.Invert(
            ref world,
            out var inverseWorld);
        
        var localRay = new Ray(
            Vector3.Transform(ray.Position, inverseWorld),
            Vector3.TransformNormal(ray.Direction, inverseWorld));

        localRay.Direction.Normalize();

        var intersection = localRay.Intersects(LocalCubeBounds);

        if (!intersection.HasValue)
        {
            return false;
        }

        distance = intersection.Value;
        localPoint = localRay.Position + localRay.Direction * distance;

        return true;
    }
    
    private CubeFace GetCubeFace(Vector3 point)
    {
        var absX = MathF.Abs(point.X);
        var absY = MathF.Abs(point.Y);
        var absZ = MathF.Abs(point.Z);

        if (absX >= absY && absX >= absZ)
        {
            return point.X > 0
                ? CubeFace.Right
                : CubeFace.Left;
        }

        if (absY >= absX && absY >= absZ)
        {
            return point.Y > 0
                ? CubeFace.Top
                : CubeFace.Bottom;
        }

        return point.Z > 0
            ? CubeFace.Front
            : CubeFace.Back;
    }
    
    private enum CubeFace
    {
        Front,
        Back,
        Left,
        Right,
        Top,
        Bottom
    }
    
    private Vector2 GetFaceUv(CubeFace face, Vector3 point)
    {
        return face switch
        {
            CubeFace.Front => new Vector2(
                point.X + 0.5f,
                0.5f - point.Y),

            CubeFace.Back => new Vector2(
                0.5f - point.X,
                0.5f - point.Y),

            CubeFace.Left => new Vector2(
                point.Z + 0.5f,
                0.5f - point.Y),

            CubeFace.Right => new Vector2(
                0.5f - point.Z, 
                0.5f - point.Y),

            CubeFace.Top => new Vector2(
                point.X + 0.5f,
                point.Z + 0.5f),

            CubeFace.Bottom => new Vector2(
                point.X + 0.5f,
                0.5f - point.Z),

            _ => Vector2.Zero
        };
    }

    public void Draw(
        SpriteBatch spriteBatch,
        DrawService drawService)
    {
        UpdateTexture();

        cube.Draw(
            world,
            view,
            projection);

        if (CurrentLevel.Type == LevelType.ThreeD)
        {
            DrawPixelNumbers3D(
                spriteBatch,
                drawService);
        }
        else
        {
            DrawPixelNumbers(
                GetImageBounds(),
                spriteBatch,
                drawService);
        }

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
        if (!_groupsByColor.TryGetValue(pixel.OriginalColor.ToColor(), out var colorGroup))
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

        _textureDirty = true;
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
        if (index < 0 || index >= _pixelLookup.Length)
        {
            return;
        }

        var pixel = _pixelLookup[index];

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
        _texturePixels[index] = color;

        _textureDirty = true;
    }

    public void SetPixelSize(float pixelWidth, float pixelHeight)
    {
        _pixelSize = new Vector2(pixelWidth, pixelHeight);
    }

    public void Replay()
    {
        if (CurrentLevel.Type == LevelType.ThreeD)
        {
            Replay3D();
            return;
        }

        foreach (var pixel in CurrentLevel.Pixels)
        {
            pixel.CurrentColor = pixel.GrayColor;
            _texturePixels[pixel.Index] =
                pixel.GrayColor.ToColor();
        }

        _textureDirty = true;

        UpdateTexture();

        _historyIndex = 0;
        _pixelsAccumulator = 0;

        ReplayLaunched = true;
    }
    
    private void Replay3D()
    {
        foreach (var pixel in CurrentLevel.Pixels)
        {
            pixel.CurrentColor = pixel.GrayColor;

            _cubeTexturePixels[pixel.Face][pixel.Index] =
                pixel.GrayColor.ToColor();
        }

        _textureDirty = true;

        UpdateTexture();

        _historyIndex = 0;
        _pixelsAccumulator = 0;

        ReplayLaunched = true;
    }
    
    private void Restart3D()
    {
        foreach (var pixel in CurrentLevel.Pixels)
        {
            pixel.CurrentColor = pixel.GrayColor;

            _cubeTexturePixels[pixel.Face][pixel.Index] =
                pixel.GrayColor.ToColor();
        }

        CurrentLevel.IsFinished = false;
        CurrentLevel.History.Clear();

        _highlightedPixels.Clear();

        _textureDirty = true;

        UpdateTexture();
    }
    
    private Vector3 GetPixelWorldPosition(
        int faceIndex,
        PixelData pixel)
    {
        var face = (CubeFace)faceIndex;

        var texture =
            CurrentLevel.CubeTextures[faceIndex];

        var width = texture.Width;
        var height = texture.Height;

        var px = pixel.Index % width;
        var py = pixel.Index / width;

        var u = (px + 0.5f) / width;
        var v = (py + 0.5f) / height;

        var local = GetLocalPointOnFace(
            face,
            u,
            v);

        return Vector3.Transform(local, world);
    }

    public void Restart()
    {
        if (CurrentLevel.Type == LevelType.ThreeD)
        {
            Restart3D();
            return;
        }

        foreach (var pixel in CurrentLevel.Pixels)
        {
            pixel.CurrentColor = pixel.GrayColor;
            _texturePixels[pixel.Index] =
                pixel.GrayColor.ToColor();
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
        _lastPaintByFace.Clear();
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
            pixel.CurrentColor = new ColorData(_highlightColor);
            _texturePixels[pixel.Index] = _highlightColor;

            _highlightedPixels.Add(pixel.Index);
        }

        _textureDirty = true;
    }
    
    private Vector3 GetLocalPointOnFace(CubeFace face, float u, float v)
    {
        // u,v в [0..1], где (0,0) — topLeft грани, (1,1) — bottomRight
        // Восстанавливаем координаты в [-0.5..0.5] для каждой оси
        var x = u - 0.5f;   // -0.5 .. 0.5
        var y = 0.5f - v;   //  0.5 .. -0.5 (верх = +0.5)
        var z = u - 0.5f;

        return face switch
        {
            CubeFace.Front  => new Vector3(x, y,  0.5f),
            CubeFace.Back   => new Vector3(-x, y, -0.5f),
            CubeFace.Left   => new Vector3(-0.5f, y,  x),  // с учётом правки: u растёт с Z
            CubeFace.Right  => new Vector3( 0.5f, y, -x),  // с учётом правки
            CubeFace.Top    => new Vector3(x,  0.5f,  v - 0.5f), // v: 0 сверху (z=-0.5) → 1 снизу (z=+0.5)
            CubeFace.Bottom => new Vector3(x, -0.5f,  v - 0.5f),
            _ => Vector3.Zero
        };
    }

    private void DrawPixelNumbers3D(
    SpriteBatch spriteBatch,
    DrawService drawService)
{
    if (CurrentLevel.Type != LevelType.ThreeD ||
        CurrentLevel.Pixels.Count == 0)
    {
        return;
    }

    var viewport = _graphicsDevice.Viewport;

    Matrix.Invert(
        ref view,
        out var invView);

    var cameraPos = invView.Translation;

    foreach (var pixel in CurrentLevel.Pixels)
    {
        if (pixel.IsFinished)
        {
            continue;
        }

        var face = (CubeFace)pixel.Face;

        var texture =
            CurrentLevel.CubeTextures[pixel.Face];

        var width = texture.Width;
        var height = texture.Height;

        var px = pixel.Index % width;
        var py = pixel.Index / width;

        var u = (px + 0.5f) / width;
        var v = (py + 0.5f) / height;

        var local = GetLocalPointOnFace(
            face,
            u,
            v);

        var worldPos =
            Vector3.Transform(local, world);

        var normal =
            Vector3.TransformNormal(
                GetFaceNormal(face),
                world);

        normal.Normalize();

        var toCamera =
            cameraPos - worldPos;

        toCamera.Normalize();

        if (Vector3.Dot(normal, toCamera) <= 0f)
        {
            continue;
        }

        var screen = viewport.Project(
            worldPos,
            projection,
            view,
            Matrix.Identity);

        if (screen.Z < 0f ||
            screen.Z > 1f)
        {
            continue;
        }

        if (!_groupsByColor.TryGetValue(
                pixel.OriginalColor.ToColor(),
                out var colorGroup))
        {
            continue;
        }

        var zoomProgress = Utils.Remap(
            _cameraService.Zoom,
            _cameraService.MinZoom,
            _cameraService.MinZoom * 2.25f,
            0f,
            1f);

        if (zoomProgress <= 0.01f)
        {
            continue;
        }

        var baseColor =
            Colors.IsDark(pixel.CurrentColor.ToColor())
                ? Color.White
                : Color.Black;

        var color = Color.Lerp(
            Color.Transparent,
            baseColor,
            zoomProgress);

        if (pixel.CurrentColor != pixel.GrayColor &&
            pixel.CurrentColor.ToColor() != _highlightColor)
        {
            color *= 0.6f;
        }

        var textScale =
            MathHelper.Lerp(
                0.5f,
                0.7f,
                zoomProgress);

        drawService.DrawString(
            spriteBatch,
            colorGroup.Number.ToString(),
            new Vector2(screen.X, screen.Y),
            color,
            textScale);
    }
}
    
    private Vector3 GetFaceNormal(CubeFace face)
    {
        return face switch
        {
            CubeFace.Front => Vector3.UnitZ,
            CubeFace.Back => -Vector3.UnitZ,
            CubeFace.Left => -Vector3.UnitX,
            CubeFace.Right => Vector3.UnitX,
            CubeFace.Top => Vector3.UnitY,
            CubeFace.Bottom => -Vector3.UnitY,
            _ => Vector3.UnitZ
        };
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
            _texturePixels[index] = pixel.GrayColor.ToColor();
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

        if (CurrentLevel.Type == LevelType.ThreeD)
        {
            for (var face = 0; face < 6; face++)
            {
                CurrentLevel.CubeTextures[face].SetData(
                    _cubeTexturePixels[face]);
            }
        }
        else
        {
            CurrentLevel.Texture.SetData(_texturePixels);
        }

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

        if (index < 0 || index >= _pixelLookup.Length)
        {
            return false;
        }

        var pixel = _pixelLookup[index];

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
}