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
    private readonly DrawService _drawService;

    public List<LevelData> Levels { get; set; } = [];
    
    private const int _levelsCount = 28;
    
    private const int _buttonSize = 128;
    private const int _lockSize = 48;
    private const int _buttonSpacing = 24;
    private const float _scrollSpeed = 0.2f;

    private int _buttonsPerRow = 3;
    private float _scroll;
    private float _targetScroll;
    
    private Texture2D _lockTexture;

    public LevelService(IServiceProvider services)
    {
        _services = services;
        _graphicsDevice = _services.GetRequiredService<GraphicsDevice>();
        _processorService = _services.GetRequiredService<PixelProcessorService>();
        _drawService = _services.GetRequiredService<DrawService>();

        _lockTexture = _services.GetRequiredService<ContentManager>().Load<Texture2D>("Icons/lock");
        
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
                bounds.Location += new Point(_buttonSize - _lockSize, level.Button.IsHovered ? -4 : 0);
                bounds.Size = new Point(_lockSize, _lockSize);
                
                spriteBatch.Draw(_lockTexture, bounds, Color.IndianRed);
            }
        }
    }
    
    public void LoadLevels(List<LevelData> savedLevels)
    {
        var useSaveData = savedLevels.Count == _levelsCount;
        
        Levels.Clear();
        for (var i = 0; i < _levelsCount; i++)
        {
            var texture = _services.GetRequiredService<ContentManager>()
                .Load<Texture2D>($"Images/img{i + 1}");

            var column = i % _buttonsPerRow;
            var row = i / _buttonsPerRow;

            var rectangle = new Rectangle(
                column * _buttonSize,
                row * _buttonSize,
                _buttonSize,
                _buttonSize);

            var level = new LevelData();

            if (useSaveData)
            {
                level = savedLevels.First(x => x.Id == i);
            }
            else if (i > 6)
            {
                level.IsLocked = true;
            }

            level.Id = i;
            level.Texture = texture;
            level.Button = new Button(texture, rectangle);
            
            Levels.Add(level);
            
            _processorService.ChangeLevel(level);
            _processorService.Generate();
        }
    }

    private void LayoutButtons()
    {
        for (var i = 0; i < Levels.Count; i++)
        {
            var column = i % _buttonsPerRow;
            var row = i / _buttonsPerRow;

            var x = _buttonSpacing + column * (_buttonSize + _buttonSpacing);
            var y = _buttonSpacing + row * (_buttonSize + _buttonSpacing) - (int)_scroll;

            Levels[i].Button.Bounds = new Rectangle(x, y, _buttonSize, _buttonSize);
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
        var contentHeight = rows * (_buttonSize + _buttonSpacing) + _buttonSpacing;

        return Math.Max(0, contentHeight - _graphicsDevice.Viewport.Height);
    }
}