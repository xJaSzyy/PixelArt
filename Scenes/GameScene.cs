using System.Collections.Generic;
using System.Linq;
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
    private GraphicsDevice _graphicsDevice;
    private SpriteBatch _spriteBatch;
    
    private SceneService _sceneService;
    private MouseService _mouseService;
    private DrawService _drawService;
    private ColorButtonsService _colorButtonsService;
    private readonly CameraService _cameraService = new();
    private readonly PixelProcessorService _processorService;

    private readonly Texture2D _imageTexture;
    private Rectangle _imageBounds;
    private float _pixelWidth;
    private float _pixelHeight;

    private const int _sizeMultiplier = 6;
    private const int _buttonSize = 56;
    private const int _buttonSpacing = 12;
    
    private Button _menuButton;

    public GameScene(Texture2D imageTexture, Rectangle imageBounds)
    {
        _imageTexture = imageTexture;
        _imageBounds = imageBounds;
        
        _processorService = new PixelProcessorService();
        _processorService.SetTexture(_imageTexture);
        _processorService.Generate();
    }

    public void Initialize(SceneService sceneService, MouseService mouseService, DrawService drawService)
    {
        _sceneService = sceneService;
        _mouseService = mouseService;
        _drawService = drawService;
    }

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        
        var pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        pixelTexture.SetData([Color.White]);

        _menuButton = new Button(pixelTexture, 
            new Rectangle(_graphicsDevice.Viewport.Width - _buttonSize - _buttonSpacing, 
                _buttonSpacing, 
                _buttonSize, 
                _buttonSize));

        _colorButtonsService = new ColorButtonsService(_graphicsDevice, _spriteBatch, _processorService);
        _colorButtonsService.LoadContent(pixelTexture);
        
        PlaceImageCenter();
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        
        if (_mouseService.IsLeftMouseButtonClicked(mouse))
        {
            _colorButtonsService.UpdateSelectedButton();
            
            if (_menuButton.IsHovered)
            {
                _sceneService.SetScene(new MenuScene());
            }
        }

        if (_mouseService.IsLeftMouseButtonPressed(mouse) && !IsMouseOverUI() && GetNumberAlpha() > 0)
        {
            PaintPixelAtMousePosition(mouse);
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

        _menuButton.Update(mouse);
        _colorButtonsService.Update(mouse);
        _cameraService.Update(mouse);
        
        _mouseService.SetMouse(mouse);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(45, 45, 45));
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        var drawBounds = GetImageBounds();

        _spriteBatch.Draw(
            _imageTexture,
            drawBounds,
            Color.White
        );

        var colorGroups = _processorService.GetPixelColorGroups();
        DrawPixelNumbers(colorGroups, drawBounds);
        
        _colorButtonsService.Draw(colorGroups, _drawService);
        
        _menuButton.Draw(_spriteBatch);

        _spriteBatch.End();
    }
    
    private void PaintPixelAtMousePosition(MouseState mouse)
    {
        var selectedButton = _colorButtonsService.GetButtons().FirstOrDefault(x => x.IsSelected);
        
        var bounds = GetImageBounds();
        if (selectedButton != null && bounds.Contains(mouse.Position))
        {
            var x = (int)((mouse.X - bounds.X) / (_pixelWidth * _cameraService.Zoom));
            var y = (int)((mouse.Y - bounds.Y) / (_pixelHeight * _cameraService.Zoom));
                
            var index = y * _imageTexture.Width + x;

            _processorService.SetPixel(index, selectedButton.Color);
        }
    }

    private bool IsMouseOverUI()
    {
        if (_colorButtonsService.GetButtons().Any(x => x.IsHovered))
        {
            return true;
        }

        if (_menuButton.IsHovered)
        {
            return true;
        }

        return false;
    }
    
    private void DrawPixelNumbers(Dictionary<Color, PixelColorGroup> colorGroups, Rectangle bounds)
    {
        foreach (var color in colorGroups.Values)
        {
            foreach (var pixel in color.Pixels.Where(pixel => !pixel.IsFinished))
            {
                _drawService.DrawString(
                    _spriteBatch, 
                    color.Number.ToString(), 
                    pixel.GetScreenPosition(bounds, _imageTexture.Width, _imageTexture.Height), 
                    Color.Lerp(
                        Color.Transparent,
                        pixel.ColorIsDark() ? Color.White : Color.Black,
                        GetNumberAlpha()
                    ),
                    _cameraService.Zoom);
            }
        }
    }
    
    private float GetNumberAlpha()
    {
        var zoom = _cameraService.Zoom;

        return MathHelper.Clamp(
            (zoom - 0.4f) / 0.4f,
            0,
            1
        );
    }

    private void PlaceImageCenter()
    {
        _imageBounds.Size *= _imageTexture.Width / 16 * _sizeMultiplier;
        
        var x = _graphicsDevice.Viewport.Width / 2 -  _imageBounds.Width / 2;
        var y = _graphicsDevice.Viewport.Height / 2 - _imageBounds.Height / 2;
        
        _imageBounds.Location = new Point(x, y);
        
        _pixelWidth = (float)_imageBounds.Width / _imageTexture.Width;
        _pixelHeight = (float)_imageBounds.Height / _imageTexture.Height;
        
        _cameraService.SetPosition(new Vector2(
            _imageBounds.X,
            _imageBounds.Y
        ));
    }
    
    private Rectangle GetImageBounds()
    {
        var width = (int)(_imageTexture.Width * _pixelWidth * _cameraService.Zoom);
        var height = (int)(_imageTexture.Height * _pixelHeight * _cameraService.Zoom);

        var cameraPosition = _cameraService.GetPosition();
        
        return new Rectangle(
            (int)cameraPosition.X,
            (int)cameraPosition.Y,
            width,
            height
        );
    }
}