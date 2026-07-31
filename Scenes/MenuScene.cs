using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    private SceneFactory _sceneFactory;
    private LevelService _levelService;

    private const int _buttonSize = 128;
    private const int _buttonSpacing = 24;
    private const float _scrollSpeed = 0.2f;

    private int _buttonsPerRow = 3;
    private float _scroll;
    private float _targetScroll;

    public MenuScene(GraphicsDevice graphicsDevice, 
        PopupService popupService, 
        SceneFactory sceneFactory, 
        SceneService sceneService, 
        MouseService mouseService, 
        DrawService drawService, 
        PlayerService playerService, 
        PixelProcessorService processorService,
        LevelService levelService)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _sceneFactory = sceneFactory;
        
        _sceneService = sceneService;
        _mouseService = mouseService;
        _drawService = drawService;
        _playerService = playerService;
        _processorService = processorService;
        _levelService = levelService;
        
        //popupService.ShowPopup();
    }

    public void LoadContent(ContentManager content)
    {
        _buttonsPerRow = Math.Max(1, (_graphicsDevice.Viewport.Width - _buttonSpacing) / (_buttonSize + _buttonSpacing));
        
        _levelService.LoadLevels(_buttonsPerRow, _buttonSize);
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
        foreach (var level in _levelService.Levels)
        {
            level.Button.Update(mouse);
        }

        _mouseService.SetMouse(mouse);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(25, 25, 25));
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        foreach (var level in _levelService.Levels)
        {
            level.Button.Draw(_spriteBatch);
        }

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