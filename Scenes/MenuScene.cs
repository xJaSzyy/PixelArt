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
    private readonly DialogService _dialogService;
    private readonly SaveService _saveService;
    private readonly PopupTextService _popupService;

    private const int _unlockLevelCost = 49;
    private const int _headerHeight = 64;
    
    private string _completedLevelsText;

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
        _dialogService = services.GetRequiredService<DialogService>();
        _saveService = services.GetRequiredService<SaveService>();
        _popupService = services.GetRequiredService<PopupTextService>();
    }

    public void LoadContent(ContentManager content)
    {
        if (_levelService.Levels.Count == 0)
        {
            var saveData = _saveService.Load();
            _levelService.LoadLevels(saveData.Levels, _headerHeight);
            _playerService.AddCoins(saveData.Coins);
        }
        else
        {
            _saveService.Save(new SaveData
            {
                Coins = _playerService.Coins,
                Levels = _levelService.Levels
            });
        }
        
        _completedLevelsText = $"{_levelService.Levels.Count(l => l.IsFinished)}/{_levelService.Levels.Count}";
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        
        if (!_dialogService.IsDialogOpen)
        {
            if (_mouseService.IsLeftMouseButtonClicked(mouse))
            {
                foreach (var level in _levelService.Levels.Where(l => l.Button.IsHovered))
                {
                    if (level.IsLocked)
                    {
                        _dialogService.ShowDialog($"Pay ${_unlockLevelCost}?", () => UnlockLevel(level));
                    }
                    else
                    {
                        _processorService.ChangeLevel(level);

                        var scene = _sceneFactory.CreateGameScene(level);
                        _sceneService.SetScene(scene);
                        break;
                    }

                }
            }

            _levelService.Update(_mouseService, mouse);
            
            _mouseService.SetMouse(mouse);
        }

        _dialogService.Update(mouse,gameTime);
        _popupService.Update(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(30, 30, 30));
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );
        
        _levelService.Draw(_spriteBatch);
        
        _drawService.DrawRectangle(_spriteBatch, 
            new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _headerHeight), 
            new Color(23, 23, 23));

        _drawService.DrawString(_spriteBatch,
            "$" + _playerService.Coins,
            new Vector2(
                _graphicsDevice.Viewport.Width / 2f,
                32
            ),
            Color.Yellow,
            2f);

        _drawService.DrawString(_spriteBatch,
            _completedLevelsText,
            new Vector2(
                _drawService.MeasureString(_completedLevelsText).X + 48,
                32
            ),
            new Color(45, 45, 45),
            2f);
        
        _popupService.Draw(_spriteBatch, _drawService.GetFont());

        _spriteBatch.End();
        
        _dialogService.Draw(_spriteBatch);
        
    }
    
    public void OnClientSizeChanged(object sender, EventArgs e)
    {
        _levelService.Resize();
    }

    public void OnGameExiting(object sender, EventArgs eventArgs)
    {
        _saveService.Save(new SaveData
        {
            Coins = _playerService.Coins,
            Levels = _levelService.Levels
        });
    }

    private bool UnlockLevel(LevelData level)
    {
        if (_playerService.RemoveCoins(_unlockLevelCost))
        {
            level.IsLocked = false;
            
            _popupService.Show(
                $"-${_unlockLevelCost}",
                new Vector2(
                    _graphicsDevice.Viewport.Width / 2f + _drawService.MeasureString("$" + _playerService.Coins).X * 1.8f,
                    32
                ),
                0.5f,
                Color.Red);
            
            return true;
        }
        
        _popupService.Show(
            "Not enough coins",
            new Vector2(
                _graphicsDevice.Viewport.Width / 2f,
                _graphicsDevice.Viewport.Height / 2f
            ),
            0.5f,
            Color.Red);

        return false;
    }
}