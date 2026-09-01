using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;
using PixelArt.Models;

namespace PixelArt.Services;

public class LevelService
{
    private readonly IServiceProvider _services;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly PixelProcessorService _processorService;
    private readonly ContentManager _contentManager;

    public List<LevelData> Levels { get; set; } = [];
    
    private const int _levelsCount = 28;
    private const int _unlockedLevelsCount = 4;
    
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

    public LevelService(IServiceProvider services)
    {
        _services = services;
        _graphicsDevice = _services.GetRequiredService<GraphicsDevice>();
        _processorService = _services.GetRequiredService<PixelProcessorService>();
        _contentManager = _services.GetRequiredService<ContentManager>();
        
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
        
        foreach (var level in Levels)
        {
            level.Button.Update(mouse);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var level in Levels)
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

        var drawService = _services.GetRequiredService<DrawService>();
        
        var useSaveData = savedLevels.Count == _levelsCount;
        
        Levels.Clear();
        for (var i = 0; i < _levelsCount; i++)
        {
            var texture = _contentManager.Load<Texture2D>($"Images/img{i + 1}");

            var level = new LevelData();

            if (useSaveData)
            {
                level = savedLevels.First(x => x.Id == i);
            }
            else if (i > _unlockedLevelsCount - 1)
            {
                level.IsLocked = true;
            }

            var clone = new Texture2D(
                _graphicsDevice,
                texture.Width,
                texture.Height,
                texture.LevelCount > 1,
                texture.Format
            );

            var data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            clone.SetData(data);
            
            level.Id = i;
            level.Texture = clone;
            level.Button = new Button(drawService, clone, Rectangle.Empty);
            
            Levels.Add(level);
            
            _processorService.SetLevel(level);
            _processorService.ProcessImage();
        }
    }

    private void LayoutButtons()
    {
        var gridOffsetX = GetGridOffsetX();

        for (var i = 0; i < Levels.Count; i++)
        {
            var column = i % _buttonsPerRow;
            var row = i / _buttonsPerRow;

            var x = gridOffsetX + column * (_buttonSize + _buttonSpacing);
            var y = _gridOffset.Y
                    + _headerHeight
                    + row * (_buttonSize + _buttonSpacing)
                    - (int)_scroll;

            Levels[i].Button.Bounds = new Rectangle(
                x,
                y,
                _buttonSize,
                _buttonSize);
        }
    }
    
    public void Resize()
    {
        _buttonsPerRow = Math.Max(1, (_graphicsDevice.Viewport.Width - _buttonSpacing) / (_buttonSize + _buttonSpacing));
        _scroll = 0f;
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
        var rows = (int)Math.Ceiling(Levels.Count / (float)_buttonsPerRow);
        var contentBottom = _gridOffset.Y + _headerHeight + rows * (_buttonSize + _buttonSpacing);
        
        return Math.Max(0, contentBottom - _graphicsDevice.Viewport.Height);
    }
    
    private int GetGridOffsetX()
    {
        var gridWidth =
            _buttonsPerRow * _buttonSize +
            (_buttonsPerRow - 1) * _buttonSpacing;

        return (_graphicsDevice.Viewport.Width - gridWidth) / 2;
    }
}