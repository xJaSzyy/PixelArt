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
        var saveService = _services.GetRequiredService<SaveService>();

        if (_levelService.Levels.Count == 0)
        {
            var saveData = saveService.Load();
            _levelService.LoadLevels(saveData.Levels);
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
        
        _levelService.Update(_mouseService, mouse);

        _mouseService.SetMouse(mouse);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(25, 25, 25));
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        _levelService.Draw(_spriteBatch);

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
        _levelService.Resize();
    }

    public void OnGameExiting(object sender, EventArgs eventArgs)
    {
        
    }
}