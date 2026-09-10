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

namespace PixelArt.Scenes;

public class GameScene : IScene
{
    private bool ColoringIsCompleted { get; set; }

    private readonly IServiceProvider _services;
    
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly MouseService _mouseService;
    private readonly KeyboardService _keyboardService;
    private readonly DrawService _drawService;
    private readonly PopupTextService _popupService;
    private readonly CameraService _cameraService;
    private readonly PixelProcessorService _processorService;
    private readonly BackgroundParticleService _backgroundService;
    private readonly LanguageService _languageService;
    private ColorButtonsService _colorButtonsService;

    private Button _homeButton;
    private Button _restartButton;
    private Button _deleteButton;

    private const int _buttonSize = 56;
    private const int _buttonSpacing = 12;
    private const int _moveSpeed = 6;
    
    private int _konamiIndex;
    private KeyboardState _previousKeyboardState;
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
        _mouseService = _services.GetRequiredService<MouseService>();
        _keyboardService = _services.GetRequiredService<KeyboardService>();
        _drawService = _services.GetRequiredService<DrawService>();
        _popupService = _services.GetRequiredService<PopupTextService>();
        _cameraService = _services.GetRequiredService<CameraService>();
        _backgroundService = _services.GetRequiredService<BackgroundParticleService>();
        _languageService = _services.GetRequiredService<LanguageService>();
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

        var pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        pixelTexture.SetData([Color.White]);

        _colorButtonsService = new ColorButtonsService(_graphicsDevice, _spriteBatch, _processorService);
        _colorButtonsService.LoadContent(pixelTexture);

        ImageToCenter();
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        var spacePressed = _keyboardService.IsKeyPressed(keyboard, Keys.Space);
        
        if (_mouseService.IsLeftMouseButtonClicked(mouse) || spacePressed)
        {
            _colorButtonsService.UpdateSelectedButton();

            if (!_processorService.ReplayLaunched && !spacePressed)
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

        HandleMoving(keyboard);
        HandleKonami(keyboard);
        HandlePainting(mouse, keyboard);
        HandleScroll(mouse, keyboard);
        
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

        if (_processorService.CurrentLevel.Type == LevelType.Custom)
        {
            _deleteButton.Update(mouse);
        }
        
        _mouseService.SetMouse(mouse);
        _keyboardService.SetState(keyboard);
    }

    private void HandleMoving(KeyboardState keyboard)
    {
        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
        {
            _cameraService.SetPosition(_cameraService.GetPosition() + new Vector2(0, _moveSpeed));
        }
        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
        {
            _cameraService.SetPosition(_cameraService.GetPosition() + new Vector2(_moveSpeed, 0));
        }
        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
        {
            _cameraService.SetPosition(_cameraService.GetPosition() + new Vector2(0, -_moveSpeed));
        }
        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
        {
            _cameraService.SetPosition(_cameraService.GetPosition() + new Vector2(-_moveSpeed, 0));
        }
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
        if ((_mouseService.IsLeftMouseButtonPressed(mouse) || _keyboardService.IsKeyPressed(keyboard, Keys.Space)) && 
            !IsMouseOverUI() && 
            Utils.Remap(_cameraService.Zoom, _cameraService.MinZoom, _cameraService.MinZoom * 2, 0f, 1f) > 0.01f)
        {
            var selectedButton = _colorButtonsService
                .GetButtons()
                .FirstOrDefault(x => x.IsSelected);

            if (selectedButton != null)
            {
                _processorService.PaintAtMousePosition(mouse, selectedButton.Color);
                
                var colorGroup = _processorService.CurrentLevel.ColorGroups.FirstOrDefault(x => x.OriginalColor == selectedButton.Color && x.IsFinished);
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
        if (_mouseService.IsScroll(mouse))
        {
            var scrollDelta = _mouseService.GetScrollDelta(mouse);
            
            if (_keyboardService.IsKeyPressed(keyboard, Keys.LeftControl))
            {
                _cameraService.ChangeZoom(mouse, scrollDelta);
            }
            else
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
                    1.25f,
                    1.5f,
                    Colors.Green,
                    2f);

                _processorService.CurrentLevel.IsFinished = true;
            }
        }
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(Colors.Background);
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        _backgroundService.Draw(_spriteBatch);
        _processorService.Draw(_spriteBatch, _drawService);

        if (!ColoringIsCompleted)
        {
            _colorButtonsService.Draw(_drawService);
        }

        if (!_processorService.ReplayLaunched)
        {
            _homeButton.Draw(_spriteBatch, Colors.Text);
            _restartButton.Draw(_spriteBatch, Colors.Text);
            
            if (_processorService.CurrentLevel.Type == LevelType.Custom)
            {
                _deleteButton.Draw(_spriteBatch, Colors.Text);
            }
        }
        
        _popupService.Draw(_spriteBatch);

        _spriteBatch.End();
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
        var languageService = _services.GetRequiredService<LanguageService>();

        _processorService.ClearHighlight();
        
        saveService.Save(new SaveData
        {
            Coins = playerService.Coins,
            Levels = levelService.Levels,
            Language = languageService.CurrentLanguage
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
}