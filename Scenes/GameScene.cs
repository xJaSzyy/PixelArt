using System;
using System.Linq;
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
using PixelArt.Services.Common;
using PixelArt.Services.Particles;
using PixelArt.Services.Pixel;

namespace PixelArt.Scenes;

public class GameScene : IScene
{
    private bool ColoringIsCompleted { get; set; }

    private readonly IServiceProvider _services;
    
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly InputService _inputService;
    private readonly DrawService _drawService;
    private readonly PopupTextService _popupService;
    private readonly CameraService _cameraService;
    private readonly PixelProcessorService _processorService;
    private readonly PixelReplayService _pixelReplayService;
    private readonly BackgroundParticleService _backgroundService;
    private readonly LanguageService _languageService;
    private ColorButtonsService _colorButtonsService;
    private readonly TooltipService _tooltipService;
    
    private SettingsData _settings;

    private Button _homeButton;
    private Button _restartButton;
    private Button _deleteButton;
    
    private readonly Texture2D[] _arrowTextures = new Texture2D[8];
    private int? _arrowTargetPixel;
    private const float _arrowTargetSwitchThreshold = 0.55f;

    private const int _buttonSize = 56;
    private const int _buttonSpacing = 12;
    private const int _minMoveSpeed = 10;
    private const int _maxMoveSpeed = 22;

    private bool _isMoving = false;
    private int _moveSpeed = 10;
    
    private KeyboardState _previousKeyboardState;
    
    private int _konamiIndex;
    private readonly Keys[] _konamiCode =
    [
        Keys.Up,
        Keys.Up,
        Keys.Down,
        Keys.Down,
        Keys.Left,
        Keys.Right,
        Keys.Left,
        Keys.Right
    ];
    
    public GameScene(IServiceProvider services)
    {
        _services = services;
        
        _processorService = _services.GetRequiredService<PixelProcessorService>();
        _graphicsDevice = _services.GetRequiredService<GraphicsDevice>();
        _spriteBatch = new SpriteBatch(_graphicsDevice);
        _inputService = _services.GetRequiredService<InputService>();
        _drawService = _services.GetRequiredService<DrawService>();
        _popupService = _services.GetRequiredService<PopupTextService>();
        _cameraService = _services.GetRequiredService<CameraService>();
        _backgroundService = _services.GetRequiredService<BackgroundParticleService>();
        _languageService = _services.GetRequiredService<LanguageService>();
        _pixelReplayService = _services.GetRequiredService<PixelReplayService>();
        _tooltipService = _services.GetRequiredService<TooltipService>();
    }

    public void LoadContent(ContentManager content)
    {
        _homeButton = new Button(_drawService, _languageService, content.Load<Texture2D>("Icons/home"),
            new Rectangle(_graphicsDevice.Viewport.Width - _buttonSize - _buttonSpacing,
                _buttonSpacing,
                _buttonSize,
                _buttonSize));
        
        _restartButton = new Button(_drawService, _languageService, content.Load<Texture2D>("Icons/restart"),
            new Rectangle(_buttonSpacing,
                _buttonSpacing,
                _buttonSize,
                _buttonSize));
        
        _deleteButton = new Button(_drawService, _languageService, content.Load<Texture2D>("Icons/delete"),
            new Rectangle(_buttonSpacing,
                _buttonSpacing + _buttonSize + _buttonSpacing,
                _buttonSize,
                _buttonSize));

        for (var i = 0; i < _arrowTextures.Length; i++)
        {
            _arrowTextures[i] = content.Load<Texture2D>($"Icons/arrow{i+1}");
        }

        var pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        pixelTexture.SetData([Color.White]);

        _colorButtonsService = new ColorButtonsService(_graphicsDevice, _spriteBatch, _processorService);
        _colorButtonsService.LoadContent(pixelTexture);

        var settingsService = _services.GetRequiredService<SettingsService>();
        _settings = settingsService.GetSettings();
        
        ImageToCenter();
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        var spacePressed = _inputService.IsKeyPressed(keyboard, Keys.Space);
        
        if (_inputService.IsLeftMouseButtonClicked(mouse) || spacePressed)
        {
            _colorButtonsService.UpdateSelectedButton();

            if (!_pixelReplayService.IsRunning && !spacePressed)
            {
                if (_homeButton.IsHovered)
                {
                    _processorService.ClearHighlight();
                    _processorService.UpdateTexture();
                    _services.GetRequiredService<SceneService>().SetScene<MenuScene>();
                }
                else if (_restartButton.IsHovered)
                {
                    Restart();
                }
                else if (_deleteButton.IsHovered && _processorService.CurrentLevel.Type == LevelType.Custom)
                {
                    _services.GetRequiredService<LevelService>().Levels.Remove(_processorService.CurrentLevel);
                    _services.GetRequiredService<SceneService>().SetScene<MenuScene>();
                }
            }
        }

        if (!_pixelReplayService.IsRunning)
        {
            HandleMoving(mouse, keyboard);
            HandleScroll(mouse, keyboard);
        }

        HandleKonami(keyboard);
        HandlePainting(mouse, keyboard);
        
        if (!ColoringIsCompleted)
        {
            _colorButtonsService.Update(mouse);
            _cameraService.Update(mouse);

            HandleColoringCompleted();
        }

        _processorService.Update(gameTime);
        _homeButton.Update(mouse);
        _restartButton.Update(mouse);
        _popupService.Update(gameTime);
        _backgroundService.Update(gameTime);
        _tooltipService.Update(mouse, [_homeButton, _restartButton, _deleteButton], ["Game.Home", "Game.Restart", "Game.Delete"]);

        if (_processorService.CurrentLevel.Type == LevelType.Custom)
        {
            _deleteButton.Update(mouse);
        }
        
        _inputService.SetState(mouse, keyboard);
    }

    private void HandleMoving(MouseState mouse, KeyboardState keyboard)
    {
        var movement = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
        {
            movement.Y += 1;
        }

        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
        {
            movement.Y -= 1;
        }

        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
        {
            movement.X += 1;
        }

        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
        {
            movement.X -= 1;
        }

        if (movement == Vector2.Zero)
        {
            _isMoving = false;
            return;
        }

        movement.Normalize();

        if (_inputService.IsScroll(mouse))
        {
            var scrollDelta = _inputService.GetScrollDelta(mouse);

            _moveSpeed += scrollDelta > 0 ? 1 : -1;

            _moveSpeed = MathHelper.Clamp(_moveSpeed, _minMoveSpeed, _maxMoveSpeed);

            _isMoving = true;
        }
        
        var speed = _moveSpeed * _cameraService.Zoom;
        
        _cameraService.SetPosition(_cameraService.GetPosition() + movement * speed);
    }

    private void HandleKonami(KeyboardState keyboard)
    {
        foreach (var key in keyboard.GetPressedKeys())
        {
            if (!_previousKeyboardState.IsKeyDown(key))
            {
                CheckKonamiCode(key);
            }
        }

        _previousKeyboardState = keyboard;
    }

    private void HandlePainting(MouseState mouse, KeyboardState keyboard)
    {
        if ((_inputService.IsLeftMouseButtonPressed(mouse) || _inputService.IsKeyPressed(keyboard, Keys.Space)) && 
            !IsMouseOverUI() && 
            Utils.Remap(_cameraService.Zoom, _cameraService.MinZoom, _cameraService.MinZoom * 2, 0f, 1f) > 0.01f)
        {
            var selectedButton = _colorButtonsService
                .GetButtons()
                .FirstOrDefault(x => x.IsSelected);

            if (selectedButton != null)
            {
                _processorService.PaintAtMousePosition(mouse, selectedButton.Color);
                
                var colorGroup = _processorService.CurrentLevel.ColorGroups
                    .FirstOrDefault(x => x.OriginalColor.ToColor() == selectedButton.Color && x.IsFinished);
                if (colorGroup != null)
                {
                    _colorButtonsService.SelectNextButton();
                }
            }
        }
        else
        {
            _processorService.ResetPainting();
        }
    }
    
    private void HandleScroll(MouseState mouse, KeyboardState keyboard)
    {
        if (_inputService.IsScroll(mouse))
        {
            var scrollDelta = _inputService.GetScrollDelta(mouse);
            
            if (_inputService.IsKeyPressed(keyboard, Keys.LeftControl))
            {
                _cameraService.ChangeZoom(mouse, scrollDelta);
            }
            else
            {
                if (!_isMoving)
                {
                    if (scrollDelta > 0)
                    {
                        _colorButtonsService.ScrollButtonsLeft();
                    }
                    else
                    {
                        _colorButtonsService.ScrollButtonsRight();
                    }
                }
            }
        }
    }
    
    private void HandleColoringCompleted()
    {
        if (_processorService.CurrentLevel.ColorGroups.All(x => x.IsFinished))
        {
            ColoringIsCompleted = true;
            ImageToCenter();
            _processorService.Replay();

            if (!_processorService.CurrentLevel.IsFinished)
            {
                var coinsToAdd = _processorService.CurrentLevel.History.Count / 10;

                _services.GetRequiredService<PlayerService>().AddCoins(coinsToAdd);

                var popupText = $"+${coinsToAdd}";

                _popupService.ShowDelayed(
                    popupText,
                    new Vector2(
                        _graphicsDevice.Viewport.Width / 2f,
                        _drawService.MeasureString(popupText).Y * 2.5f
                    ),
                    _pixelReplayService.ReplayDuration,
                    1.5f,
                    Colors.Accept,
                    2f);

                _processorService.CurrentLevel.IsFinished = true;
            }
        }
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(Colors.Background);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _backgroundService.Draw(_spriteBatch);
        _processorService.Draw(_spriteBatch, _drawService);

        if (!ColoringIsCompleted)
        {
            _colorButtonsService.Draw(_drawService);
        }

        if (!_pixelReplayService.IsRunning)
        {
            _homeButton.Draw(_spriteBatch);
            _restartButton.Draw(_spriteBatch);

            if (_processorService.CurrentLevel.Type == LevelType.Custom)
            {
                _deleteButton.Draw(_spriteBatch);
            }
        }

        DrawArrow();

        _popupService.Draw(_spriteBatch);
        _tooltipService.Draw(_spriteBatch);

        _spriteBatch.End();
    }

    private void DrawArrow()
    {
        var screenCenter = new Vector2(_graphicsDevice.Viewport.Width / 2f, _graphicsDevice.Viewport.Height / 2f);

        if (_settings.ArrowHintEnabled && !_processorService.HasHighlightedPixelOnScreen())
        {
            if (_arrowTargetPixel.HasValue && _processorService.TryGetHighlightedPixelScreenPosition(_arrowTargetPixel.Value, out var currentTargetPosition))
            {
                if (!_processorService.ContainsHighlightedPixel(_arrowTargetPixel.Value))
                {
                    _arrowTargetPixel = null;
                    return;
                }
            }
            else
            {
                _arrowTargetPixel = null;

                if (!_processorService.TryGetNearestHighlightedPixel(screenCenter, out var nearestIndex, out currentTargetPosition))
                {
                    return;
                }

                _arrowTargetPixel = nearestIndex;
            }

            if (_processorService.TryGetNearestHighlightedPixel(screenCenter, out var newNearestIndex, out var newNearestPosition))
            {
                var currentDistanceSquared = Vector2.DistanceSquared(screenCenter, currentTargetPosition);
                var newDistanceSquared = Vector2.DistanceSquared(screenCenter, newNearestPosition);

                const float thresholdSquared = _arrowTargetSwitchThreshold * _arrowTargetSwitchThreshold;

                if (newNearestIndex != _arrowTargetPixel.Value && newDistanceSquared < currentDistanceSquared * thresholdSquared)
                {
                    _arrowTargetPixel = newNearestIndex;
                    currentTargetPosition = newNearestPosition;
                }
            }

            var direction = currentTargetPosition - screenCenter;
            var angle = MathF.Atan2(direction.Y, direction.X);
            var directionIndex = (int)MathF.Round(angle / (MathF.PI / 4f));
            directionIndex = (directionIndex + 8) % 8;

            var arrowTexture = _arrowTextures[directionIndex];

            var arrowPosition = GetArrowPosition(screenCenter, currentTargetPosition);

            var backgroundPixelIsDark = _processorService.PixelIsDark(arrowPosition);

            _spriteBatch.Draw(
                arrowTexture,
                arrowPosition,
                null,
                backgroundPixelIsDark ? Colors.Text : Colors.Black,
                0f,
                new Vector2(
                    arrowTexture.Width / 2f,
                    arrowTexture.Height / 2f),
                80f / arrowTexture.Width,
                SpriteEffects.None,
                0f);
        }
        else
        {
            _arrowTargetPixel = null;
        }
    }
    
    public void OnClientSizeChanged(object sender, EventArgs e)
    {
        _homeButton.Bounds = new Rectangle(_graphicsDevice.Viewport.Width - _buttonSize - _buttonSpacing,
            _buttonSpacing,
            _buttonSize,
            _buttonSize);
        
        _colorButtonsService.ResetScroll();
        
        ImageToCenter();
    }

    public void OnGameExiting(object sender, EventArgs eventArgs)
    {
        var saveService = _services.GetRequiredService<SaveService>();
        var levelService = _services.GetRequiredService<LevelService>();
        var playerService = _services.GetRequiredService<PlayerService>();

        _processorService.ClearHighlight();
        
        saveService.Save(new SaveData
        {
            Coins = playerService.Coins,
            Levels = levelService.Levels,
            Settings = _settings
        });
    }
    
    private void Restart()
    {
        _processorService.Restart();
        
        ColoringIsCompleted = false;
        _colorButtonsService.SelectButton(0);

        ImageToCenter();
    }

    private bool IsMouseOverUI()
    {
        if (_colorButtonsService.GetButtons().Any(x => x.IsHovered))
        {
            return true;
        }

        if (_homeButton.IsHovered || _restartButton.IsHovered || _deleteButton.IsHovered)
        {
            return true;
        }

        return false;
    }

    private void ImageToCenter()
    {
        var level = _processorService.CurrentLevel;

        const float baseTextureSize = 32f;
        const float baseZoom = 1f;

        var textureSize = Math.Max(level.Texture.Width, level.Texture.Height);
        var zoom = baseZoom * baseTextureSize / textureSize;

        _cameraService.SetZoomBounds(zoom * 0.2f, zoom * 1.8f, zoom * 0.05f);
        
        _cameraService.Zoom = _cameraService.MinZoom;

        var bounds = level.Button.Bounds;
        
        bounds.Size *= level.Texture.Width / 2;
        bounds.Location = Point.Zero;

        _processorService.SetPixelSize((float)bounds.Width / level.Texture.Width,
            (float)bounds.Height / level.Texture.Height);

        var imageBounds = _processorService.GetImageBounds();
        
        _cameraService.SetPosition(new Vector2(
            bounds.X + _graphicsDevice.Viewport.Width * 0.5f - imageBounds.Width * 0.5f,
            bounds.X + _graphicsDevice.Viewport.Height * 0.5f - imageBounds.Height * 0.5f
        ));
    }
    
    private void CheckKonamiCode(Keys key)
    {
        if (key == _konamiCode[_konamiIndex])
        {
            _konamiIndex++;

            if (_konamiIndex == _konamiCode.Length)
            {
                _konamiIndex = 0;

                _processorService.BrushRadius = _processorService.BrushRadius == 0 ? 1 : 0;
            }
        }
        else
        {
            _konamiIndex = 0;
        }
    }
    
    private Vector2 GetArrowPosition(Vector2 screenCenter, Vector2 targetPosition)
    {
        var direction = targetPosition - screenCenter;

        if (direction == Vector2.Zero)
        {
            return screenCenter;
        }

        direction.Normalize();

        const float arrowSize = 64f;
        const float padding = 8f;

        var left = arrowSize + padding;
        var right = _graphicsDevice.Viewport.Width - padding;
        var top = arrowSize + padding;
        var bottom = _graphicsDevice.Viewport.Height - padding;

        var tX = float.MaxValue;
        var tY = float.MaxValue;

        if (direction.X > 0)
        {
            tX = (right - screenCenter.X) / direction.X;
        }
        else if (direction.X < 0)
        {
            tX = (left - screenCenter.X) / direction.X;
        }

        if (direction.Y > 0)
        {
            tY = (bottom - screenCenter.Y) / direction.Y;
        }
        else if (direction.Y < 0)
        {
            tY = (top - screenCenter.Y) / direction.Y;
        }

        var t = MathF.Min(tX, tY);

        var position = screenCenter + direction * t;

        return position - new Vector2(arrowSize / 2f);
    }
}