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
    private readonly LanguageService _languageService;

    private Button _languageButton;

    private const int _unlockLevelCost = 49;
    private const int _headerHeight = 64;
    private const float _headerTextScale = 1.5f;
    private const int _headerProgressBarHeight = 8;
    private const int _headerProgressBarExtraWidth = 48;
    private const int _headerElementsPadding = 8;
    
    
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
        _languageService = services.GetRequiredService<LanguageService>();
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

        _languageButton = new Button(
            _drawService,
            null,
            Rectangle.Empty)
        {
            Text = _languageService.CurrentLanguage.ShortName,
            TextColor = Colors.Text,
            TextScale = _headerTextScale,
            Font = _drawService.GetFont()
        };
        
        _levelService.Resize();
        ResizeLanguageButton();
        
        _completedLevelsCount = _levelService.Levels.Count(l => l.IsFinished);
        _totalLevelsCount = _levelService.Levels.Count;
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();

        if (_mouseService.IsLeftMouseButtonClicked(mouse))
        {
            if (_languageButton.IsHovered)
            {
                _languageService.ChangeLanguage();
                ResizeLanguageButton();
                _dialogService.SetText($"{_languageService.GetText("Menu.Pay")} ${_unlockLevelCost}?");
            }
        }

        if (!_dialogService.IsDialogOpen)
        {
            if (_mouseService.IsLeftMouseButtonClicked(mouse))
            {
                foreach (var level in _levelService.Levels.Where(l => l.Button.IsHovered))
                {
                    if (level.IsLocked)
                    {
                        _dialogService.ShowDialog($"{_languageService.GetText("Menu.Pay")} ${_unlockLevelCost}?",
                            () => UnlockLevel(level));
                    }
                    else
                    {
                        _processorService.SetLevel(level);
                        _sceneService.SetScene<GameScene>();
                        break;
                    }
                }
            }
        }

        _levelService.Update(_mouseService, mouse);
        _languageButton.Update(mouse);
        _dialogService.Update(mouse, gameTime);
        _popupService.Update(gameTime);
        _backgroundService.Update(gameTime);

        _mouseService.SetMouse(mouse);
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
                
        _popupService.Draw(_spriteBatch);

        _spriteBatch.End();
    }

    private void DrawHeader()
    {
        _drawService.DrawRectangle(_spriteBatch, 
            new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _headerHeight), 
            Colors.Background);

        var center = new Vector2(_graphicsDevice.Viewport.Width / 2f, 32);
        var coinsText = "$" + _playerService.Coins;
        
        var completedLevelsText = $"{_completedLevelsCount}/{_totalLevelsCount}";

        _drawService.DrawString(_spriteBatch,
            completedLevelsText,
            new Vector2(center.X, center.Y - _headerElementsPadding),
            Colors.Text,
            1.25f);
        
        var progressBarSize = new Point((int)(_drawService.MeasureString(completedLevelsText).X + _headerProgressBarExtraWidth), _headerProgressBarHeight);
        var progressBarLocation = new Point((int)(center.X - progressBarSize.X * .5f), (int)(center.Y + _headerElementsPadding));
        
        _drawService.DrawProgressBar(_spriteBatch, new Rectangle(progressBarLocation, progressBarSize), 
            _totalLevelsCount > 0 ? (float)_completedLevelsCount / _totalLevelsCount : 0f, 
            Colors.Text, 
            Colors.Text, 
            Colors.Green,
            2);
        
        _drawService.DrawString(_spriteBatch,
            coinsText,
            new Vector2(_graphicsDevice.Viewport.Width - _drawService.MeasureString(coinsText).X - _headerProgressBarExtraWidth, center.Y),
            Colors.Yellow,
            2f);
        
        _languageButton.Draw(_spriteBatch);
    }

    public void OnClientSizeChanged(object sender, EventArgs e)
    {
        ResizeLanguageButton();
        _levelService.Resize();
    }

    private void ResizeLanguageButton()
    {
        var language = _languageService.CurrentLanguage.ShortName;
        
        _languageButton.Text = language;

        var stringSize = _drawService.MeasureString(language, _headerTextScale);
        var textSize = new Point((int)MathF.Ceiling(stringSize.X), (int)MathF.Ceiling(stringSize.Y));

        _languageButton.Bounds = new Rectangle(
            _headerProgressBarExtraWidth,
            20,
            textSize.X + 2,
            textSize.Y + 2
        );
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
                1.25f,
                Colors.Red);
            
            return true;
        }
        
        _popupService.Show(
            _languageService.GetText("Menu.NotEnoughCoins"),
            new Vector2(
                _graphicsDevice.Viewport.Width / 2f,
                _graphicsDevice.Viewport.Height / 2f
            ),
            0.5f,
            Colors.Red);

        return false;
    }
}