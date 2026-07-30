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

    private const int _levelsCount = 25;
    private List<LevelData> _levels = [];
    private const int _buttonSize = 128;
    
    public void Initialize(SceneService sceneService, MouseService mouseService, DrawService drawService)
    {
        _sceneService = sceneService;
        _mouseService = mouseService;

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

        var buttonsPerRow = Math.Max(1, graphicsDevice.Viewport.Width / _buttonSize);
        for (var i = 0; i < _levels.Count; i++)
        {
            var level = _levels[i];
            
            var texture = content.Load<Texture2D>($"Images/{imageNames[i]}");

            var column = i % buttonsPerRow;
            var row = i / buttonsPerRow;

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

        _levels = _levels.OrderBy(_ => Guid.NewGuid()).ToList();
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();

        _levels.ForEach(l => l.Button.Update(mouse));

        if (_mouseService.IsLeftMouseButtonClicked(mouse))
        {
            foreach (var level in _levels.Where(l => l.Button.IsHovered))
            {
                _processorService.ChangeLevel(level);
                _sceneService.SetScene(new GameScene(level, _processorService, this));
                break;
            }
        }

        _mouseService.SetMouse(mouse);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(25, 25, 25));
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        _levels.ForEach(l => l.Button.Draw(_spriteBatch));

        _spriteBatch.End();
    }
}