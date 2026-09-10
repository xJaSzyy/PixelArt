#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;
using PixelArt.Enums;
using PixelArt.Models;

namespace PixelArt.Services;

public class LevelService
{
    private readonly IServiceProvider _services;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly PixelProcessorService _processorService;
    private readonly ContentManager _contentManager;
    private readonly DrawService _drawService;
    private readonly LanguageService _languageService;

    public List<LevelData> Levels { get; set; } = [];
    public LevelType CurrentLevelType { get; set; } = LevelType.Animal;
    
    private const int _unlockedLevelsCount = 3;
    
    private const int _buttonSize = 128;
    private const int _iconSize = 40;
    private const int _buttonSpacing = 24;
    private const float _scrollSpeed = 0.2f;

    private int _buttonsPerRow = 3;
    private float _scroll;
    private float _targetScroll;
    private readonly Point _gridOffset = new(32, 32);
    private int _headerHeight;
    
    private readonly Texture2D _lockTexture;
    private readonly Texture2D _checkTexture;
    
    private readonly Dictionary<LevelType, (string Folder, int Count)> _levelSets = new()
    {
        [LevelType.Animal] = ("Images/Animal", 9),
        [LevelType.App] = ("Images/App", 16),
        [LevelType.Clothes] = ("Images/Clothes", 16),
        [LevelType.Drink] = ("Images/Drink", 16),
        [LevelType.Fish] = ("Images/Fish", 16),
        [LevelType.Food] = ("Images/Food", 12),
        [LevelType.Sword] = ("Images/Sword", 15)
    };

    public LevelService(IServiceProvider services)
    {
        _services = services;
        _graphicsDevice = _services.GetRequiredService<GraphicsDevice>();
        _processorService = _services.GetRequiredService<PixelProcessorService>();
        _contentManager = _services.GetRequiredService<ContentManager>();
        _drawService = _services.GetRequiredService<DrawService>();
        _languageService = _services.GetRequiredService<LanguageService>();
        
        _lockTexture = _contentManager.Load<Texture2D>("Icons/lock");
        _checkTexture = _contentManager.Load<Texture2D>("Icons/check");
        
        Resize();
    }
    
    public void Update(MouseService mouseService, MouseState mouse)
    {
        _scroll = MathHelper.Lerp(_scroll, _targetScroll, _scrollSpeed);
        
        if (mouseService.IsScroll(mouse))
        {
            var scrollDelta = mouseService.GetScrollDelta(mouse);

            if (scrollDelta > 0)
            {
                ScrollUp();
            }
            else
            {
                ScrollDown();
            }
        }
        
        LayoutButtons();

        var isMouseOverHeader = IsMouseOverHeader(mouse);
        
        foreach (var level in Levels.Where(l => l.Type == CurrentLevelType))
        {
            level.Button.Update(mouse, !isMouseOverHeader);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var level in Levels.Where(l => l.Type == CurrentLevelType))
        {
            level.Button.Draw(spriteBatch);

            if (level.IsLocked)
            {
                var bounds = level.Button.Bounds;
                bounds.Location += new Point(_buttonSize - _iconSize, level.Button.IsHovered ? -2 : 0);
                bounds.Size = new Point(_iconSize, _iconSize);
                
                spriteBatch.Draw(_lockTexture, bounds, Colors.Red);
            }
            else if (level.IsFinished)
            {
                var bounds = level.Button.Bounds;
                bounds.Location += new Point(_buttonSize - _iconSize, level.Button.IsHovered ? -2 : 0);
                bounds.Size = new Point(_iconSize, _iconSize);

                spriteBatch.Draw(_checkTexture, bounds, Colors.Green);
            }
        }
    }
    
    public void LoadLevels(List<LevelData> savedLevels, int headerHeight)
    {
        _headerHeight = headerHeight;
        
        var useSaveData = savedLevels.Count(x => x.Type != LevelType.Custom) == _levelSets.Values.Sum(x => x.Count);
        
        Levels.Clear();
        
        foreach (var (type, data) in _levelSets)
        {
            for (var i = 0; i < data.Count; i++)
            {
                var originalTexture = _contentManager.Load<Texture2D>($"{data.Folder}/{i + 1}");

                LevelData? savedLevel = null;
                var isLocked = false;
                
                if (useSaveData)
                {
                    savedLevel = savedLevels.First(x => x.Id == i && x.Type == type);
                    isLocked = savedLevel.IsLocked;
                }
                else if (i > _unlockedLevelsCount - 1)
                {
                    isLocked = true;
                }
                
                AddLevel(originalTexture, i, type, savedLevel, isLocked);
            }
        }

        foreach (var savedLevel in savedLevels.Where(x => x.Type == LevelType.Custom))
        {
            var originalTexture = _contentManager.Load<Texture2D>($"Images/Custom/{savedLevel.Id}");
            
            AddLevel(originalTexture, savedLevel.Id, savedLevel.Type, savedLevel);
        }
    }

    private void AddLevel(Texture2D originalTexture, 
        int levelId, 
        LevelType type, 
        LevelData? savedLevel = null, 
        bool isLocked = false)
    {
        var texture = ColorQuantizer.Quantize(_graphicsDevice, originalTexture, 32);
        
        var level = new LevelData();

        if (savedLevel != null)
        {
            level = savedLevel;
        }

        level.Id = levelId;
        level.Type = type;
        level.IsLocked = isLocked;
        level.Texture = Utils.CloneTexture2D(_graphicsDevice, texture);
        level.GrayTexture = Utils.CloneTexture2D(_graphicsDevice, texture);
        level.OriginalTexture = texture;
        level.Button = new Button(_drawService, _languageService, level.Texture, Rectangle.Empty);
            
        Levels.Add(level);

        _processorService.SetLevel(level);
        _processorService.ProcessImage();
    }

    public void AddCustomLevel(Texture2D originalTexture, 
        int levelId, 
        LevelType type, 
        LevelData? savedLevel = null, 
        bool isLocked = false)
    {
        AddLevel(originalTexture, levelId, type, savedLevel, isLocked);
        
        var path = Path.Combine(AppContext.BaseDirectory, "Content", "Images", "Custom", $"{levelId}.png");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var stream = File.Create(path);

        originalTexture.SaveAsPng(stream, originalTexture.Width, originalTexture.Height);
    }
    
    private void LayoutButtons()
    {
        var gridOffsetX = GetGridOffsetX();

        var visibleLevels = Levels
            .Where(l => l.Type == CurrentLevelType)
            .ToList();

        for (var i = 0; i < visibleLevels.Count; i++)
        {
            var column = i % _buttonsPerRow;
            var row = i / _buttonsPerRow;

            var x = gridOffsetX + column * (_buttonSize + _buttonSpacing);
            var y = _gridOffset.Y
                    + _headerHeight
                    + row * (_buttonSize + _buttonSpacing)
                    - (int)_scroll;

            visibleLevels[i].Button.Bounds = new Rectangle(x, y, _buttonSize, _buttonSize);
        }
    }
    
    public void Resize()
    {
        _buttonsPerRow = Math.Max(1, (_graphicsDevice.Viewport.Width - _buttonSpacing) / (_buttonSize + _buttonSpacing));
        _scroll = 0f;
        _targetScroll = 0f;
    }
    
    public LevelData? GetHoveredLevel()
    {
        return Levels
            .Where(l => l.Type == CurrentLevelType)
            .FirstOrDefault(l => l.Button.IsHovered);
    }

    public void ResetScroll()
    {
        _targetScroll = 0f;
    }
    
    private void ScrollDown()
    {
        _targetScroll += _buttonSize + _buttonSpacing;
        _targetScroll = Math.Min(_targetScroll, GetMaxScroll());
    }

    private void ScrollUp()
    {
        _targetScroll -= _buttonSize + _buttonSpacing;
        _targetScroll = Math.Max(_targetScroll, 0);
    }
    
    private float GetMaxScroll()
    {
        var rows = (int)Math.Ceiling(Levels.Count(l => l.Type == CurrentLevelType) / (float)_buttonsPerRow);
        var contentBottom = _gridOffset.Y + _headerHeight + rows * (_buttonSize + _buttonSpacing);
        
        return Math.Max(0, contentBottom - _graphicsDevice.Viewport.Height);
    }
    
    private int GetGridOffsetX()
    {
        var gridWidth = _buttonsPerRow * _buttonSize + (_buttonsPerRow - 1) * _buttonSpacing;

        return (_graphicsDevice.Viewport.Width - gridWidth) / 2;
    }
    
    private bool IsMouseOverHeader(MouseState mouse)
    {
        return mouse.Position.Y < _headerHeight;
    }
    
    public Rectangle GetNextLevelBounds(int width, int height, bool applyScroll = false)
    {
        var visibleLevelsCount = Levels.Count(l => l.Type == CurrentLevelType);

        var column = visibleLevelsCount % _buttonsPerRow;
        var row = visibleLevelsCount / _buttonsPerRow;

        var x = GetGridOffsetX()
                + column * (_buttonSize + _buttonSpacing);

        var y = _gridOffset.Y
                + _headerHeight
                + row * (_buttonSize + _buttonSpacing);

        if (applyScroll)
            y -= (int)_scroll;

        var centerX = x + _buttonSize / 2;
        var centerY = y + _buttonSize / 2;

        return new Rectangle(
            centerX - width / 2,
            centerY - height / 2,
            width,
            height
        );
    }
}