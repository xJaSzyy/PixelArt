#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NativeFileDialogSharp;
using PixelArt.Buttons;
using PixelArt.Enums;
using PixelArt.Interfaces;
using PixelArt.Models;
using PixelArt.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

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
    private readonly ImageLoaderService _imageLoaderService;

    private Button _languageButton;
    private Button _testButton;
    private Texture2D? _testTexture;

    private const int _unlockLevelCost = 49;
    private const int _headerHeight = 64;
    private const int _typeButtonsHeight = 32;
    private const float _headerTextScale = 1.5f;
    private const int _headerProgressBarHeight = 8;
    private const int _headerProgressBarExtraWidth = 48;
    private const int _headerElementsPadding = 8;
    private const int _typeButtonsSpacing = 24;
    
    private int _completedLevelsCount;
    private int _totalLevelsCount;

    private readonly List<Button> _typeButtons = [];

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
        _imageLoaderService = _services.GetRequiredService<ImageLoaderService>();
    }

    public void LoadContent(ContentManager content)
    {
        if (_levelService.Levels.Count == 0)
        {
            var saveData = _saveService.Load();
            _levelService.LoadLevels(saveData.Levels, _headerHeight + _typeButtonsHeight);
            _playerService.AddCoins(saveData.Coins);
            _languageService.SetLanguage(saveData.Language);
        }
        else
        {
            _saveService.Save(new SaveData
            {
                Coins = _playerService.Coins,
                Levels = _levelService.Levels,
                Language = _languageService.CurrentLanguage
            });
        }

        _languageButton = new Button(
            _drawService,
            null,
            Rectangle.Empty)
        {
            Text = _languageService.CurrentLanguage.ShortName,
            TextColor = Colors.Text,
            TextScale = _headerTextScale
        };
        
        _testButton = new Button(
            _drawService,
            null,
            new Rectangle(new Point(0, 0), new Point(64, 64)))
        {
            Text = "+",
            TextColor = Colors.Text,
            TextScale = _headerTextScale
        };
        
        foreach (var type in Enum.GetNames<LevelType>())
        {
            _typeButtons.Add(new Button(
                _drawService,
                null,
                Rectangle.Empty)
            {
                Text = type,
                TextColor = Colors.Text,
                TextScale = 1f
            });
        }
        
        _levelService.Resize();
        ResizeLanguageButton();
        ResizeTypeButtons();

        UpdateLevelProgress();
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
                if (!IsMouseOverHeader(mouse))
                {
                    var hoveredLevel = _levelService.GetHoveredLevel();
                    if (hoveredLevel != null)
                    {
                        if (hoveredLevel.IsLocked)
                        {
                            _dialogService.ShowDialog($"{_languageService.GetText("Menu.Pay")} ${_unlockLevelCost}?", () => UnlockLevel(hoveredLevel));
                            _mouseService.SetMouse(mouse);
                        }
                        else
                        {
                            _processorService.SetLevel(hoveredLevel);
                            _sceneService.SetScene<GameScene>();
                        }
                    }
                }
                
                foreach (var typeButton in _typeButtons)
                {
                    if (typeButton.IsHovered && typeButton.Text != null)
                    {
                        _levelService.CurrentLevelType = Enum.Parse<LevelType>(typeButton.Text);
                        _levelService.ResetScroll();
                        UpdateLevelProgress();
                        break;
                    }
                }

                if (_testButton.IsHovered)
                {
                    OpenImageAsync();
                }
            }
        }
        
        foreach (var typeButton in _typeButtons)
        {
            var text = _levelService.CurrentLevelType.ToString();

            if (typeButton.Text != null && typeButton.Text == text)
            {
                typeButton.IsSelected = true;
            }
            else
            {
                typeButton.IsSelected = false;
            }
        }
        
        if (_filePickerTask != null && _filePickerTask.IsCompleted)
        {
            var path = _filePickerTask.Result;

            _filePickerTask = null;

            if (path != null)
            {
                _testTexture?.Dispose();
                _testTexture = _imageLoaderService.LoadTexture(_graphicsDevice, path);
            }
        }

        _levelService.Update(_mouseService, mouse);
        _languageButton.Update(mouse);
        _testButton.Update(mouse);
        _dialogService.Update(mouse, gameTime);
        _popupService.Update(gameTime);
        _backgroundService.Update(gameTime);
        _typeButtons.ForEach(b => b.Update(mouse));

        _mouseService.SetMouse(mouse);
    }
    
    private Task<string?>? _filePickerTask;
    
    private void OpenImageAsync()
    {
        if (_filePickerTask != null)
        {
            return;
        }

        _filePickerTask = Task.Run(() => _imageLoaderService.PickImage());
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

        if (_testTexture != null)
        {
            _spriteBatch.Draw(
                _testTexture,
                new Vector2(100, 100),
                Color.White
            );
        }

        _typeButtons.ForEach(b => b.Draw(_spriteBatch));
        _testButton.Draw(_spriteBatch);
        _popupService.Draw(_spriteBatch);

        _spriteBatch.End();
    }

    private void DrawHeader()
    {
        _drawService.DrawRectangle(_spriteBatch, 
            new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _headerHeight + _typeButtonsHeight), 
            Colors.Background);

        var center = new Vector2(_graphicsDevice.Viewport.Width / 2f, 32);
        var coinsText = "$" + _playerService.Coins;
        
        var completedLevelsText = $"{_completedLevelsCount}/{_totalLevelsCount}";
        var progress = _totalLevelsCount > 0 ? (float)_completedLevelsCount / _totalLevelsCount : 0f;

        _drawService.DrawString(_spriteBatch,
            completedLevelsText,
            new Vector2(center.X, center.Y - (progress < 1 ? _headerElementsPadding : -1)),
            Colors.Text, 
            progress >= 1 ? _headerTextScale : 1.25f);

        if (progress < 1)
        {
            var progressBarSize = new Point((int)(_drawService.MeasureString(completedLevelsText).X + _headerProgressBarExtraWidth), _headerProgressBarHeight);
            var progressBarLocation = new Point((int)(center.X - progressBarSize.X * .5f), (int)(center.Y + _headerElementsPadding));

            _drawService.DrawProgressBar(_spriteBatch, new Rectangle(progressBarLocation, progressBarSize),
                progress,
                Colors.Text,
                Colors.Text,
                Colors.Green,
                2);
        }

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
        ResizeTypeButtons();
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
    
    private void ResizeTypeButtons()
    {
        var types = Enum.GetNames<LevelType>();

        var sizes = types
            .Select(type => _drawService.MeasureString(type))
            .ToList();

        var widths = sizes
            .Select(size => (int)MathF.Ceiling(size.X))
            .ToList();

        var heights = sizes
            .Select(size => (int)MathF.Ceiling(size.Y))
            .ToList();

        var totalWidth = widths.Sum()
                         + _typeButtonsSpacing * (types.Length - 1);

        var x = (_graphicsDevice.Viewport.Width - totalWidth) / 2;
        var y = _headerHeight + (_typeButtonsHeight - heights.Max()) / 2;

        for (var i = 0; i < _typeButtons.Count; i++)
        {
            _typeButtons[i].Bounds = new Rectangle(
                x,
                y,
                widths[i],
                heights[i]);

            x += widths[i] + _typeButtonsSpacing;
        }
    }

    public void OnGameExiting(object sender, EventArgs eventArgs)
    {
        _saveService.Save(new SaveData
        {
            Coins = _playerService.Coins,
            Levels = _levelService.Levels,
            Language = _languageService.CurrentLanguage
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
    
    private bool IsMouseOverHeader(MouseState mouse)
    {
        return mouse.Position.Y < _headerHeight + _typeButtonsHeight;
    }

    private void UpdateLevelProgress()
    {
        _completedLevelsCount = _levelService.Levels
            .Where(x => x.Type == _levelService.CurrentLevelType)
            .Count(l => l.IsFinished);
        
        _totalLevelsCount = _levelService.Levels
            .Count(x => x.Type == _levelService.CurrentLevelType);
    }
}