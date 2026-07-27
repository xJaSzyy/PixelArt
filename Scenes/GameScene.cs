using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;
using PixelArt.Interfaces;
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

    private const int _sizeMultiplier = 6;
    private const int _buttonSize = 56;
    private const int _buttonSpacing = 12;
    
    private Button _homeButton;

    public GameScene(Texture2D imageTexture, Rectangle imageBounds)
    {
        _imageTexture = imageTexture;
        _imageBounds = imageBounds;
        
        _processorService = new PixelProcessorService(_cameraService);
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
        
        
        var homeTexture = content.Load<Texture2D>("Icons/home");
        _homeButton = new Button(homeTexture, 
            new Rectangle(_graphicsDevice.Viewport.Width - _buttonSize - _buttonSpacing, 
                _buttonSpacing, 
                _buttonSize, 
                _buttonSize));

        var pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        pixelTexture.SetData([Color.White]);
        
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
            
            if (_homeButton.IsHovered)
            {
                _sceneService.SetScene(new MenuScene());
            }
        }

        if (_mouseService.IsLeftMouseButtonPressed(mouse) && !IsMouseOverUI() && _processorService.GetNumberAlpha() > 0)
        {
            var selectedButton = _colorButtonsService.GetButtons().FirstOrDefault(x => x.IsSelected);
            _processorService.PaintPixelAtMousePosition(mouse, selectedButton);
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

        _homeButton.Update(mouse);
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

        _processorService.Draw(_spriteBatch, _drawService);
        
        _colorButtonsService.Draw(_drawService);
        
        _homeButton.Draw(_spriteBatch);

        _spriteBatch.End();
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

    private void PlaceImageCenter()
    {
        _imageBounds.Size *= _imageTexture.Width / 16 * _sizeMultiplier;
        
        var x = _graphicsDevice.Viewport.Width / 2 -  _imageBounds.Width / 2;
        var y = _graphicsDevice.Viewport.Height / 2 - _imageBounds.Height / 2;
        
        _imageBounds.Location = new Point(x, y);

        _processorService.SetPixelSize((float)_imageBounds.Width / _imageTexture.Width,
            (float)_imageBounds.Height / _imageTexture.Height);
        
        _cameraService.SetPosition(new Vector2(
            _imageBounds.X,
            _imageBounds.Y
        ));
    }
}