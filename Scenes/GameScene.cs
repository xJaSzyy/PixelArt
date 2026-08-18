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

public class GameScene : IScene
{
    private bool ColoringIsCompleted { get; set; }

    private readonly IServiceProvider _services;
    
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly MouseService _mouseService;
    private readonly DrawService _drawService;
    private readonly CameraService _cameraService = new();
    private readonly PixelProcessorService _processorService;
    private ColorButtonsService _colorButtonsService;

    private readonly LevelData _level;
    private Button _homeButton;
    private Button _restartButton;

    private const int _buttonSize = 56;
    private const int _buttonSpacing = 12;

    private readonly string[] _resultText = new string[2];

    public GameScene(LevelData level, IServiceProvider services)
    {
        _services = services;
        
        _level = level;
        _processorService = _services.GetRequiredService<PixelProcessorService>();
        _graphicsDevice = _services.GetRequiredService<GraphicsDevice>();
        _spriteBatch = new SpriteBatch(_graphicsDevice);
        _mouseService = _services.GetRequiredService<MouseService>();
        _drawService = _services.GetRequiredService<DrawService>();
    }

    public void LoadContent(ContentManager content)
    {
        _homeButton = new Button(content.Load<Texture2D>("Icons/home"),
            new Rectangle(_graphicsDevice.Viewport.Width - _buttonSize - _buttonSpacing,
                _buttonSpacing,
                _buttonSize,
                _buttonSize));
        
        _restartButton = new Button(content.Load<Texture2D>("Icons/restart"),
            new Rectangle(_buttonSpacing,
                _buttonSpacing,
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
        
        if (_mouseService.IsLeftMouseButtonClicked(mouse) || keyboard.IsKeyDown(Keys.Space))
        {
            _colorButtonsService.UpdateSelectedButton();

            if (!_processorService.ReplayLaunched)
            {
                if (_homeButton.IsHovered)
                {
                    _colorButtonsService.ClearHighlight(true);
                    _services.GetRequiredService<SceneService>().SetScene<MenuScene>();
                }
                else if (_restartButton.IsHovered)
                {
                    Restart();
                }
            }
        }

        if ((_mouseService.IsLeftMouseButtonPressed(mouse) || keyboard.IsKeyDown(Keys.Space)) && !IsMouseOverUI() && 
            Utils.Remap(_cameraService.Zoom, _cameraService.MinZoom, _cameraService.MinZoom * 2, 0f, 1f) > 0.01f)
        {
            var selectedButton = _colorButtonsService.GetButtons().FirstOrDefault(x => x.IsSelected);
            if (selectedButton != null)
            {
                _processorService.PaintPixelAtMousePosition(mouse, selectedButton.Color, _cameraService);
            }
        }
        
        if (_mouseService.IsScroll(mouse))
        {
            var scrollDelta = _mouseService.GetScrollDelta(mouse);
            
            if (keyboard.IsKeyDown(Keys.LeftControl))
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
        
        if (!ColoringIsCompleted)
        {
            _colorButtonsService.Update(mouse);
            _cameraService.Update(mouse);
            
            if (_processorService.CurrentLevel.ColorGroups.All(x => x.IsFinished))
            {
                ColoringIsCompleted = true;
                ImageToCenter();
                _processorService.Replay();

                if (!_processorService.CurrentLevel.IsFinished)
                {
                    var coinsToAdd = _level.History.Count / 10;

                    switch (_level.ErrorCountPercent)
                    {
                        case <= 5f:
                            coinsToAdd *= 2;
                            _resultText[0] = "Perfect!";

                            break;

                        case <= 30f:
                            _resultText[0] = string.Empty;
                            break;

                        default:
                            coinsToAdd /= 2;
                            _resultText[0] = "Too many mistakes.";
                            break;
                    }

                    _resultText[1] = $"+{coinsToAdd} Pixoins";
                    _services.GetRequiredService<PlayerService>().AddCoins(coinsToAdd);

                    _processorService.CurrentLevel.IsFinished = true;
                }
                else
                {
                    _resultText[0] = string.Empty;
                    _resultText[1] = string.Empty;
                }
            }
        }

        _processorService.Update(gameTime);
        _homeButton.Update(mouse);
        _restartButton.Update(mouse);
        _mouseService.SetMouse(mouse);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(45, 45, 45));
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        _processorService.Draw(_spriteBatch, _drawService, _cameraService);

        if (!ColoringIsCompleted)
        {
            _colorButtonsService.Draw(_drawService);
        }
        else if (!_processorService.ReplayLaunched)
        {
            if (_resultText[0] == string.Empty)
            {
                _drawService.DrawString(_spriteBatch,
                    _resultText[1],
                    new Vector2(_graphicsDevice.Viewport.Width * .5f,
                        _drawService.MeasureString(_resultText[1]).Y + _buttonSpacing),
                    Color.Yellow,
                    2f
                );
            }
            else 
            {
                _drawService.DrawString(_spriteBatch,
                    _resultText[0],
                    new Vector2(_graphicsDevice.Viewport.Width * .5f,
                        _drawService.MeasureString(_resultText[0]).Y + _buttonSpacing),
                    Color.Yellow,
                    2f
                );

                _drawService.DrawString(_spriteBatch,
                    _resultText[1],
                    new Vector2(_graphicsDevice.Viewport.Width * .5f,
                        _drawService.MeasureString(_resultText[0]).Y + _buttonSpacing +
                        _drawService.MeasureString(_resultText[1]).Y * 2 + _buttonSpacing),
                    Color.Yellow,
                    2f
                );
            }
        }

        if (!_processorService.ReplayLaunched)
        {
            _homeButton.Draw(_spriteBatch);
            _restartButton.Draw(_spriteBatch);
        }

        _spriteBatch.End();
    }
    
    public void OnClientSizeChanged(object sender, EventArgs e)
    {
        _homeButton.Bounds = new Rectangle(_graphicsDevice.Viewport.Width - _buttonSize - _buttonSpacing,
            _buttonSpacing,
            _buttonSize,
            _buttonSize);
        
        ImageToCenter();
    }

    public void OnGameExiting(object sender, EventArgs eventArgs)
    {
        var saveService = _services.GetRequiredService<SaveService>();
        var levelService = _services.GetRequiredService<LevelService>();
        var playerService = _services.GetRequiredService<PlayerService>();

        _colorButtonsService.ClearHighlight(true);
        
        saveService.Save(new SaveData
        {
            Coins = playerService.Coins,
            Levels = levelService.Levels
        });
    }
    
    private void Restart()
    {
        _processorService.Restart();
        
        ColoringIsCompleted = false;
        _colorButtonsService.SelectButton(0);
    }

    private bool IsMouseOverUI()
    {
        if (_colorButtonsService.GetButtons().Any(x => x.IsHovered))
        {
            return true;
        }

        if (_homeButton.IsHovered)
        {
            return true;
        }

        return false;
    }

    private void ImageToCenter()
    {
        _cameraService.Zoom = _cameraService.MinZoom;
        
        var bounds = _level.Button.Bounds;
        
        bounds.Size *= _level.Texture.Width / 2;
        bounds.Location = Point.Zero;

        _processorService.SetPixelSize((float)bounds.Width / _level.Texture.Width,
            (float)bounds.Height / _level.Texture.Height);

        var imageBounds = _processorService.GetImageBounds(_cameraService);
        
        _cameraService.SetPosition(new Vector2(
            bounds.X + _graphicsDevice.Viewport.Width * 0.5f - imageBounds.Width * 0.5f,
            bounds.X + _graphicsDevice.Viewport.Height * 0.5f - imageBounds.Height * 0.5f
        ));
    }
}