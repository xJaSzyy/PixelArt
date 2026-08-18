using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Interfaces;
using PixelArt.Models;
using PixelArt.Services;

namespace PixelArt.Scenes;

public class MenuScene : IScene
{
    private readonly IServiceProvider _services;
    
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    
    private readonly SceneService _sceneService;
    private readonly MouseService _mouseService;
    private readonly PixelProcessorService _processorService;
    private readonly DrawService _drawService;
    private readonly PlayerService _playerService;
    private readonly SceneFactory _sceneFactory;
    private readonly LevelService _levelService;

    private const int _buttonSize = 128;
    private const int _buttonSpacing = 24;
    private const float _scrollSpeed = 0.2f;

    private int _buttonsPerRow = 3;
    private float _scroll;
    private float _targetScroll;

    public MenuScene(IServiceProvider services)
    {
        _services = services;
        
        _graphicsDevice = services.GetRequiredService<GraphicsDevice>();
        _spriteBatch = new SpriteBatch(_graphicsDevice);
        _sceneFactory = services.GetRequiredService<SceneFactory>();
        _sceneService = services.GetRequiredService<SceneService>();
        
        _mouseService = services.GetRequiredService<MouseService>();
        _drawService = services.GetRequiredService<DrawService>();
        _playerService = services.GetRequiredService<PlayerService>();
        _processorService = services.GetRequiredService<PixelProcessorService>();
        _levelService = services.GetRequiredService<LevelService>();
    }

    public void LoadContent(ContentManager content)
    {
        _buttonsPerRow = Math.Max(1, (_graphicsDevice.Viewport.Width - _buttonSpacing) / (_buttonSize + _buttonSpacing));

        var saveService = _services.GetRequiredService<SaveService>();

        if (_levelService.Levels.Count == 0)
        {
            var saveData = saveService.Load();
            _levelService.TryLoadLevels(_buttonsPerRow, _buttonSize, saveData.Levels);
            _playerService.AddCoins(saveData.Coins);
        }
        else
        {
            saveService.Save(new SaveData
            {
                Coins = _playerService.Coins,
                Levels = _levelService.Levels
            });
        }
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        
        if (_mouseService.IsLeftMouseButtonClicked(mouse))
        {
            foreach (var level in _levelService.Levels.Where(l => l.Button.IsHovered))
            {
                 _processorService.ChangeLevel(level);
                
                var scene = _sceneFactory.CreateGameScene(level);
                _sceneService.SetScene(scene);
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
        _levelService.Levels.ForEach(l => l.Button.Update(mouse));

        _mouseService.SetMouse(mouse);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(25, 25, 25));
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        _levelService.Levels.ForEach(l => l.Button.Draw(_spriteBatch));

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
    
    public void OnClientSizeChanged(object sender, EventArgs e)
    {
        _buttonsPerRow = Math.Max(1, (_graphicsDevice.Viewport.Width - _buttonSpacing) / (_buttonSize + _buttonSpacing));
        _scroll = 0f;
        _targetScroll = 0f;
    }

    public void OnGameExiting(object sender, EventArgs eventArgs)
    {
        
    }

    private void LayoutButtons()
    {
        for (var i = 0; i < _levelService.Levels.Count; i++)
        {
            var column = i % _buttonsPerRow;
            var row = i / _buttonsPerRow;

            var x = _buttonSpacing + column * (_buttonSize + _buttonSpacing);
            var y = _buttonSpacing + row * (_buttonSize + _buttonSpacing) - (int)_scroll;

            _levelService.Levels[i].Button.Bounds = new Rectangle(x, y, _buttonSize, _buttonSize);
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
        var rows = (int)Math.Ceiling(_levelService.Levels.Count / (float)_buttonsPerRow);
        var contentHeight = rows * (_buttonSize + _buttonSpacing) + _buttonSpacing;

        return Math.Max(0, contentHeight - _graphicsDevice.Viewport.Height);
    }
}