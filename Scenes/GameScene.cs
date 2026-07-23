using System.Collections.Generic;
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
    private readonly PixelProcessorService _processorService;

    private readonly Texture2D _imageTexture;
    private Rectangle _imageBounds;
    
    private Texture2D _pixelTexture;
    private readonly List<ColorButton> _buttons = [];
    private const int _buttonSize = 56;
    private const int _buttonSpacing = 12;

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
        
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);
        
        PlaceImageCenter();
        CreateColorButtons();
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();

        if (_mouseService.IsRightMouseButtonClicked(mouse))
        {
            _sceneService.SetScene(new MenuScene());
        }
        
        _buttons.ForEach(x => x.Update(mouse));
        
        _mouseService.SetMouse(mouse);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(45, 45, 45));
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        _spriteBatch.Draw(
            _imageTexture,
            _imageBounds,
            Color.White
        );

        var colorMap = _processorService.GetColorMap();
        
        foreach (var color in colorMap.Values)
        {
            var numberColor = color.GrayColorIsDark() ? Color.White : Color.Black;
            
            foreach (var pixel in color.Pixels)
            {
                _drawService.DrawString(
                    _spriteBatch, 
                    color.Number.ToString(), 
                    pixel.GetScreenPosition(_imageBounds, _imageTexture.Width, _imageTexture.Height), 
                    numberColor);
            }
        }
        
        foreach (var colorButton in _buttons)
        {
            colorButton.Draw(_spriteBatch, _pixelTexture);
            
            _drawService.DrawString(
                _spriteBatch, 
                colorButton.Number.ToString(), 
                colorButton.GetDrawBounds().Center.ToVector2(), 
                colorButton.ColorIsDark() ? Color.White : Color.Black);
        }

        _spriteBatch.End();
    }
    
    private void PlaceImageCenter()
    {
        _imageBounds.Size *= 4;
        
        var x = _graphicsDevice.Viewport.Width / 2 -  _imageBounds.Width / 2;
        var y = _graphicsDevice.Viewport.Height / 2 - _imageBounds.Height / 2;
        
        _imageBounds.Location = new Point(x, y);
    }
    
    private void CreateColorButtons()
    {
        var x = 0;
        var y = _graphicsDevice.Viewport.Height - _buttonSize;

        foreach (var group in _processorService.GetColorMap().Values)
        {
            _buttons.Add(new ColorButton(group.OriginalColor, group.Number, new Rectangle(x, y, _buttonSize, _buttonSize)));
            x += _buttonSize + _buttonSpacing;
        }
    }
}