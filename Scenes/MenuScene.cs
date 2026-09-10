#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;
using PixelArt.Enums;
using PixelArt.Interfaces;
using PixelArt.Models;
using PixelArt.Services;
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
    
    private Button _loadImageButton;
    private Texture2D? _loadedImageTexture;
    private Task<string?>? _filePickerTask;

    private readonly Point _buttonSize = new(64, 64);

    private const int _unlockLevelCost = 49;
    private const int _headerHeight = 64;
    private const float _headerTextScale = 1.5f;
    private const int _headerProgressBarHeight = 8;
    private const int _headerProgressBarExtraWidth = 48;
    private const int _headerElementsPadding = 8;
    private const int _typeButtonsSpacing = 24;
    private const int _typeButtonHeight = 32;
    private const int _typeButtonHorizontalPadding = 8;
    private const int _typeButtonsRowSpacing = 4;
    private const int _typeButtonsSidePadding = 16;
    private int _typeButtonsHeight;
    
    private int LevelsHeaderHeight => _headerHeight + _typeButtonsHeight;
    
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
            _levelService.LoadLevels(saveData.Levels, LevelsHeaderHeight);
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
            _languageService,
            null,
            Rectangle.Empty)
        {
            TextKey = _languageService.CurrentLanguage.ShortName,
            TextColor = Colors.Text,
            TextScale = _headerTextScale
        };
        
        _loadImageButton = new Button(
            _drawService,
            _languageService,
            content.Load<Texture2D>("Icons/plus"),
            Rectangle.Empty);
        
        foreach (var type in Enum.GetNames<LevelType>())
        {
            _typeButtons.Add(new Button(
                _drawService,
                _languageService,
                null,
                Rectangle.Empty)
            {
                TextKey = $"Menu.{type}",
                TextColor = Colors.Text,
                TextScale = 1f
            });
        }

        ResizeAll();
        UpdateLevelProgress();
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();

        if (_mouseService.IsLeftMouseButtonClicked(mouse))
        {
            if (_languageButton.IsHovered)
            {
                ChangeLanguage();
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
                    if (typeButton is { IsHovered: true, TextKey: not null })
                    {
                        _levelService.CurrentLevelType = Enum.Parse<LevelType>(typeButton.TextKey.Replace("Menu.", ""));
                        _levelService.ResetScroll();
                        UpdateLevelProgress();
                        ResizeAll();
                        break;
                    }
                }

                if (_loadImageButton.IsHovered)
                {
                    OpenImageAsync();
                }
            }
        }
        
        foreach (var typeButton in _typeButtons)
        {
            var type = _levelService.CurrentLevelType.ToString();

            if (typeButton.TextKey != null && typeButton.TextKey == $"Menu.{type}")
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
                _loadedImageTexture?.Dispose();
                _loadedImageTexture = _imageLoaderService.LoadTexture(_graphicsDevice, path);
                if (_loadedImageTexture != null)
                {
                    _levelService.AddCustomLevel(
                        _loadedImageTexture, 
                        _levelService.Levels.Count(x => x.Type == LevelType.Custom), 
                        LevelType.Custom);
                    ResizeAll();
                }
            }
        }

        _levelService.Update(_mouseService, mouse);
        _languageButton.Update(mouse);
        _dialogService.Update(mouse, gameTime);
        _popupService.Update(gameTime);
        _backgroundService.Update(gameTime);
        _typeButtons.ForEach(b => b.Update(mouse));

        if (_levelService.CurrentLevelType == LevelType.Custom)
        {
            _loadImageButton.Update(mouse);
        }
        
        _mouseService.SetMouse(mouse);
    }

    private void ChangeLanguage()
    {
        _languageService.ChangeLanguage();
        ResizeLanguageButton();
        _dialogService.SetText($"{_languageService.GetText("Menu.Pay")} ${_unlockLevelCost}?");
        ResizeTypeButtons();
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

        _typeButtons.ForEach(b => b.Draw(_spriteBatch));
        _popupService.Draw(_spriteBatch);
        
        if (_levelService.CurrentLevelType == LevelType.Custom)
        {
            _loadImageButton.Draw(_spriteBatch);
        }

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
        ResizeAll();
    }

    private void ResizeAll()
    {
        ResizeLanguageButton();
        ResizeTypeButtons();
        _levelService.SetHeaderHeight(LevelsHeaderHeight);
        _levelService.Resize();
        _loadImageButton.Bounds = _levelService.GetNextLevelBounds(_buttonSize.X, _buttonSize.Y);
    }

    private void ResizeLanguageButton()
    {
        var language = _languageService.CurrentLanguage.ShortName;
        
        _languageButton.TextKey = language;

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
        var viewportWidth = _graphicsDevice.Viewport.Width;

        var sizes = _typeButtons
            .Select(button => _drawService.MeasureString(
                _languageService.GetText(button.TextKey)))
            .ToList();

        var widths = sizes
            .Select(size => (int)MathF.Ceiling(size.X) + _typeButtonHorizontalPadding * 2)
            .ToList();

        var rows = new List<List<int>>();
        var currentRow = new List<int>();
        var currentWidth = 0;

        var availableWidth = viewportWidth - _typeButtonsSidePadding * 2;

        for (var i = 0; i < widths.Count; i++)
        {
            var requiredWidth = currentRow.Count == 0
                ? widths[i]
                : currentWidth + _typeButtonsSpacing + widths[i];

            if (currentRow.Count > 0 && requiredWidth > availableWidth)
            {
                rows.Add(currentRow);

                currentRow = [];
                currentWidth = 0;
            }

            currentRow.Add(i);

            currentWidth = currentRow.Count == 1
                ? widths[i]
                : currentWidth + _typeButtonsSpacing + widths[i];
        }

        if (currentRow.Count > 0)
        {
            rows.Add(currentRow);
        }

        var totalHeight =
            rows.Count * _typeButtonHeight +
            (rows.Count - 1) * _typeButtonsRowSpacing;

        _typeButtonsHeight = totalHeight;

        var y = _headerHeight;

        foreach (var row in rows)
        {
            var rowWidth =
                row.Sum(index => widths[index]) +
                _typeButtonsSpacing * (row.Count - 1);

            var x = (viewportWidth - rowWidth) / 2;

            foreach (var index in row)
            {
                _typeButtons[index].Bounds = new Rectangle(x, y, widths[index], _typeButtonHeight);

                x += widths[index] + _typeButtonsSpacing;
            }

            y += _typeButtonHeight + _typeButtonsRowSpacing;
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
    
    private void OpenImageAsync()
    {
        if (_filePickerTask != null)
        {
            return;
        }

        _filePickerTask = Task.Run(() => ImageLoaderService.PickImage());
    }
}