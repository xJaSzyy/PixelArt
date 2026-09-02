using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;
using PixelArt.Interfaces;
using PixelArt.Services;

namespace PixelArt.Scenes;

public class GameScene2 : IScene
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
    private readonly BackgroundParticleService _backgroundService;
    private readonly PixelService _pixelService;

    private Button _homeButton;
    private Button _restartButton;

    private const int _buttonSize = 56;
    private const int _buttonSpacing = 12;
    
    public GameScene2(IServiceProvider services)
    {
        _services = services;
        
        _graphicsDevice = _services.GetRequiredService<GraphicsDevice>();
        _spriteBatch = new SpriteBatch(_graphicsDevice);
        _mouseService = _services.GetRequiredService<MouseService>();
        _keyboardService = _services.GetRequiredService<KeyboardService>();
        _drawService = _services.GetRequiredService<DrawService>();
        _popupService = _services.GetRequiredService<PopupTextService>();
        _cameraService = _services.GetRequiredService<CameraService>();
        _backgroundService = _services.GetRequiredService<BackgroundParticleService>();
        _pixelService = _services.GetRequiredService<PixelService>();

        _cameraService.SetZoomBounds(1f, 2.5f, .2f);
    }

    public void LoadContent(ContentManager content)
    {
        _homeButton = new Button(_drawService,content.Load<Texture2D>("Icons/home"),
            new Rectangle(_graphicsDevice.Viewport.Width - _buttonSize - _buttonSpacing,
                _buttonSpacing,
                _buttonSize,
                _buttonSize));
        
        _restartButton = new Button(_drawService,content.Load<Texture2D>("Icons/restart"),
            new Rectangle(_buttonSpacing,
                _buttonSpacing,
                _buttonSize,
                _buttonSize));

        var texture = _services.GetRequiredService<PixelProcessorService>().CurrentLevel.OriginalTexture;
        var clone = Utils.CloneTexture2D(_graphicsDevice, texture);
        
        _pixelService.LoadContent(content, clone);

        _pixelService.Center(
            _graphicsDevice.Viewport.Width,
            _graphicsDevice.Viewport.Height);
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        if (_mouseService.IsLeftMouseButtonClicked(mouse))
        {
            if (_homeButton.IsHovered)
            {
                _services.GetRequiredService<SceneService>().SetScene<MenuScene>();
            }
            else if (_restartButton.IsHovered)
            {
                Restart();
            }
        } 
        else if (_keyboardService.IsKeyUpOnce(keyboard, Keys.Space))
        {
            _pixelService.CheckContourMatch();
        }

        HandlePainting(mouse, keyboard);
        HandleScroll(mouse, keyboard);
        
        if (!ColoringIsCompleted)
        {
            _cameraService.Update(mouse);
        }

        _homeButton.Update(mouse);
        _restartButton.Update(mouse);
        _popupService.Update(gameTime);
        _backgroundService.Update(gameTime);
        
        _mouseService.SetMouse(mouse);
        _keyboardService.SetState(keyboard);
    }

    private void HandlePainting(MouseState mouse, KeyboardState keyboard)
    {
        if ((_mouseService.IsLeftMouseButtonPressed(mouse) || 
             _keyboardService.IsKeyPressed(keyboard, Keys.Space)) &&
            !IsMouseOverUI())
        {
            var mousePosition = mouse.Position.ToVector2();
            _pixelService.TryPaint(mousePosition);
        }
    }
    
    private void HandleScroll(MouseState mouse, KeyboardState keyboard)
    {
        if (!_mouseService.IsScroll(mouse) || !_keyboardService.IsKeyPressed(keyboard, Keys.LeftControl))
        {
            return;
        }
        
        var scrollDelta = _mouseService.GetScrollDelta(mouse);
        _cameraService.ChangeZoom(mouse, scrollDelta);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(Colors.Background);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _backgroundService.Draw(_spriteBatch);

        _pixelService.Draw(_spriteBatch);

        _homeButton.Draw(_spriteBatch, Colors.Text);
        _restartButton.Draw(_spriteBatch, Colors.Text);
        _popupService.Draw(_spriteBatch);

        _spriteBatch.End();
    }

    public void OnClientSizeChanged(object sender, EventArgs e)
    {
        _homeButton.Bounds = new Rectangle(
            _graphicsDevice.Viewport.Width - _buttonSize - _buttonSpacing,
            _buttonSpacing,
            _buttonSize,
            _buttonSize);

        _pixelService.Center(
            _graphicsDevice.Viewport.Width,
            _graphicsDevice.Viewport.Height);
    }

    public void OnGameExiting(object sender, EventArgs eventArgs)
    {
        
    }
    
    private void Restart()
    {
        ColoringIsCompleted = false;
        _pixelService.Reset();
    }

    private bool IsMouseOverUI()
    {

        if (_homeButton.IsHovered || _restartButton.IsHovered)
        {
            return true;
        }

        return false;
    }
}