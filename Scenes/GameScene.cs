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
    private bool ColoringIsCompleted { get; set; }
    
    private GraphicsDevice _graphicsDevice;
    private SpriteBatch _spriteBatch;
    
    private SceneService _sceneService;
    private MouseService _mouseService;
    private DrawService _drawService;
    private ColorButtonsService _colorButtonsService;
    private readonly CameraService _cameraService = new();
    private readonly PixelProcessorService _processorService;
    private readonly IScene _menuScene;

    private readonly LevelData _level;

    private const int _buttonSize = 56;
    private const int _buttonSpacing = 12;
    
    private Button _homeButton;

    public GameScene(LevelData level, PixelProcessorService processorService, IScene menuScene)
    {
        _level = level;
        _processorService = processorService;
        _menuScene = menuScene;
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
                _colorButtonsService.ClearHighlight(true);
                _sceneService.SetScene(_menuScene);
            }
        }

        if (_mouseService.IsLeftMouseButtonPressed(mouse) && !IsMouseOverUI() && Utils.Remap(_cameraService.Zoom, _cameraService.MinZoom, _cameraService.MinZoom * 2, 0f, 1f) > 0)
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
            
            if (_processorService.CurrentLevel.ColorGroups.All(x => x.Value.IsFinished))
            {
                ColoringIsCompleted = true;
                PlaceImageCenter();
            }
        }

        _homeButton.Update(mouse);
        _processorService.ApplyPixelChanges();
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