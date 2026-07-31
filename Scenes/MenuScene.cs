using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;
using PixelArt.Interfaces;
using PixelArt.Models;
using PixelArt.Services;

namespace PixelArt.Scenes;

public class MenuScene : IScene
{
    private GraphicsDevice _graphicsDevice;
    private SpriteBatch _spriteBatch;
    
    private SceneService _sceneService;
    private MouseService _mouseService;
    private PixelProcessorService _processorService;
    private DrawService _drawService;
    private PlayerService _playerService;

    private List<LevelData> _levels = [];
    
    private const int _levelsCount = 28;
    private const int _buttonSize = 128;
    private const int _buttonSpacing = 24;
    private const float _scrollSpeed = 0.2f;

    private int _buttonsPerRow = 3;
    private float _scroll;
    private float _targetScroll;
    
    public void Initialize(SceneService sceneService, MouseService mouseService, DrawService drawService, PlayerService playerService)
    {
        _sceneService = sceneService;
        _mouseService = mouseService;
        _drawService = drawService;
        _playerService = playerService;

        if (_levels.Count == _levelsCount)
        {
            return;
        }
        
        _levels.Clear();
        for (var i = 0; i < _levelsCount; i++)
        {
            _levels.Add(new LevelData
            {
                IsLoaded = false,
            });
        }
    }

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;

        _spriteBatch = new SpriteBatch(graphicsDevice);

        if (_levels.All(l => l.IsLoaded))
        {
            return;
        }
        
        _processorService = new PixelProcessorService();
        
        var imageNames = Enumerable.Range(1, _levelsCount)
            .Select(i => $"img{i}")
            .ToList();

        _buttonsPerRow = Math.Max(1, (graphicsDevice.Viewport.Width - _buttonSpacing) / (_buttonSize + _buttonSpacing));
        for (var i = 0; i < _levels.Count; i++)
        {
            var level = _levels[i];
            
            var texture = content.Load<Texture2D>($"Images/{imageNames[i]}");

            var column = i % _buttonsPerRow;
            var row = i / _buttonsPerRow;

            var rectangle = new Rectangle(
                column * _buttonSize,
                row * _buttonSize,
                _buttonSize,
                _buttonSize);

            level.Id = i;
            level.Texture = texture;
            level.Button = new Button(texture, rectangle);
            level.IsLoaded = true;

            _processorService.ChangeLevel(level);
            _processorService.Generate();
        }
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        
        if (_mouseService.IsLeftMouseButtonClicked(mouse))
        {
            foreach (var level in _levels.Where(l => l.Button.IsHovered))
            {
                _processorService.ChangeLevel(level);
                _sceneService.SetScene(new GameScene(level, _processorService, this));
                break;
            }
        }
        
        _scroll = MathHelper.Lerp(_scroll, _targetScroll, _scrollSpeed);
        
        if (_mouseService.IsScroll(mouse))
        {
            var scrollDelta = _mouseService.GetScrollDelta(mouse);

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
        _levels.ForEach(l => l.Button.Update(mouse));

        _mouseService.SetMouse(mouse);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(25, 25, 25));
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        _levels.ForEach(l => l.Button.Draw(_spriteBatch));

        var scale = 2.5f;
        var text = _playerService.Coins.ToString();

        var position = new Vector2(
            _graphicsDevice.Viewport.Width / 2f,
            48
        );

        _drawService.DrawString(
            _spriteBatch,
            text,
            position,
            Color.Yellow,
            scale
        );

        _spriteBatch.End();
    }
    
    private void LayoutButtons()
    {
        for (var i = 0; i < _levels.Count; i++)
        {
            var column = i % _buttonsPerRow;
            var row = i / _buttonsPerRow;

            var x = _buttonSpacing + column * (_buttonSize + _buttonSpacing);
            var y = _buttonSpacing + row * (_buttonSize + _buttonSpacing) - (int)_scroll;

            _levels[i].Button.Bounds = new Rectangle(x, y, _buttonSize, _buttonSize);
        }
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
        var rows = (int)Math.Ceiling(_levels.Count / (float)_buttonsPerRow);
        var contentHeight = rows * (_buttonSize + _buttonSpacing) + _buttonSpacing;

        return Math.Max(0, contentHeight - _graphicsDevice.Viewport.Height);
    }
}