using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceProvider _services;
    
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    
    private readonly SceneService _sceneService;
    private readonly MouseService _mouseService;
    private readonly PixelProcessorService _processorService;
    private readonly DrawService _drawService;
    private readonly PlayerService _playerService;
    private readonly LevelService _levelService;
    private readonly DialogService _dialogService;
    private readonly SaveService _saveService;
    private readonly PopupTextService _popupService;
    private readonly BackgroundParticleService _backgroundService;

    private Button _languageButton;

    private const int _unlockLevelCost = 49;
    private const int _headerHeight = 64;
    
    private int _completedLevelsCount;
    private int _totalLevelsCount;

    public MenuScene(IServiceProvider services)
    {
        _services = services;
        
        _graphicsDevice = services.GetRequiredService<GraphicsDevice>();
        _spriteBatch = new SpriteBatch(_graphicsDevice);
        _sceneService = services.GetRequiredService<SceneService>();
        
        _mouseService = services.GetRequiredService<MouseService>();
        _drawService = services.GetRequiredService<DrawService>();
        _playerService = services.GetRequiredService<PlayerService>();
        _processorService = services.GetRequiredService<PixelProcessorService>();
        _levelService = services.GetRequiredService<LevelService>();
        _dialogService = services.GetRequiredService<DialogService>();
        _saveService = services.GetRequiredService<SaveService>();
        _popupService = services.GetRequiredService<PopupTextService>();
        _backgroundService = services.GetRequiredService<BackgroundParticleService>();
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

        const string languageText = "RU";
        const float textScale = 1.5f;

        var textSize = _drawService.MeasureString(languageText, textScale);

        var textWidth = (int)MathF.Ceiling(textSize.X);
        var textHeight = (int)MathF.Ceiling(textSize.Y);

        _languageButton = new Button(
            _drawService,
            null,
            new Rectangle(
                _graphicsDevice.Viewport.Width - textWidth - 2 - 16,
                20,
                textWidth + 2,
                textHeight + 2
            ))
        {
            Text = languageText,
            TextColor = Colors.LightBackground,
            TextScale = textScale,
            Font = _drawService.GetFont()
        };
        
        _completedLevelsCount = _levelService.Levels.Count(l => l.IsFinished);
        _totalLevelsCount = _levelService.Levels.Count;
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
                        _processorService.SetLevel(level);
                        _sceneService.SetScene<GameScene>();
                        break;
                    }

                }
            }

            _levelService.Update(_mouseService, mouse);
            
            _mouseService.SetMouse(mouse);
        }

        _languageButton.Update(mouse);
        _dialogService.Update(mouse, gameTime);
        _popupService.Update(gameTime);
        _backgroundService.Update(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(Colors.Background);
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );
        
        _backgroundService.Draw(_spriteBatch);

        _levelService.Draw(_spriteBatch);
        _dialogService.Draw(_spriteBatch);

        DrawHeader();
                
        _popupService.Draw(_spriteBatch, _drawService.GetFont());

        _spriteBatch.End();
    }

    private void DrawHeader()
    {
        _drawService.DrawRectangle(_spriteBatch, 
            new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _headerHeight), 
            Colors.DarkBackground);

        var completedLevelsText = $"{_completedLevelsCount}/{_totalLevelsCount}";
        
        _drawService.DrawString(_spriteBatch,
            completedLevelsText,
            new Vector2(
                _drawService.MeasureString(completedLevelsText).X + 112,
                32
            ),
            Colors.LightBackground,
            1.5f);
        
        _drawService.DrawProgressBar(_spriteBatch, 
            new Rectangle(new Point(16, 24), 
                new Point((int)(_drawService.MeasureString(completedLevelsText).X + 40), 16)), 
            _totalLevelsCount > 0 ? (float)_completedLevelsCount / _totalLevelsCount : 0f, 
            Colors.LightBackground, 
            Colors.LightBackground, 
            Colors.Green);
        
        _drawService.DrawString(_spriteBatch,
            "$" + _playerService.Coins,
            new Vector2(
                _graphicsDevice.Viewport.Width / 2f,
                32
            ),
            Colors.Yellow,
            2f);
        
        _languageButton.Draw(_spriteBatch);
    }

    public void OnClientSizeChanged(object sender, EventArgs e)
    {
        const string languageText = "RU";
        const float textScale = 1.5f;

        var textSize = _drawService.MeasureString(languageText, textScale);

        var textWidth = (int)MathF.Ceiling(textSize.X);
        var textHeight = (int)MathF.Ceiling(textSize.Y);

        _languageButton.Bounds = new Rectangle(
            _graphicsDevice.Viewport.Width - textWidth - 2 - 16,
            20,
            textWidth + 2,
            textHeight + 2
        );
        
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
                Colors.Red);
            
            return true;
        }
        
        _popupService.Show(
            "Not enough coins",
            new Vector2(
                _graphicsDevice.Viewport.Width / 2f,
                _graphicsDevice.Viewport.Height / 2f
            ),
            0.5f,
            Colors.Red);

        return false;
    }
}