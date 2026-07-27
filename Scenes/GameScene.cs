using System;
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
    private readonly CameraService _cameraService = new();
    private readonly PixelProcessorService _processorService;

    private readonly Texture2D _imageTexture;
    private Rectangle _imageBounds;
    private float _pixelWidth;
    private float _pixelHeight;

    private Texture2D _pixelTexture;
    private readonly List<ColorButton> _colorButtons = [];
    private int _visibleStartIndex;
    private int VisibleButtons => Math.Max(1,
        (_graphicsDevice.Viewport.Width - _buttonSpacing * 2) / (_buttonSize + _buttonSpacing));
    
    private const int _buttonSize = 56;
    private const int _buttonSpacing = 12;
    private const int _sizeMultiplier = 6;

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
        
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);

        _menuButton = new Button(_pixelTexture, 
            new Rectangle(_graphicsDevice.Viewport.Width - _buttonSize - _buttonSpacing, 
                _buttonSpacing, 
                _buttonSize, 
                _buttonSize));
        
        PlaceImageCenter();
        CreateColorButtons();
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        
        if (_mouseService.IsLeftMouseButtonClicked(mouse))
        {
            UpdateSelectedButton();
            
            if (_menuButton.IsHovered)
            {
                _sceneService.SetScene(new MenuScene());
            }
        }

        if (_mouseService.IsLeftMouseButtonPressed(mouse) && !IsMouseOverUI())
        {
            PaintPixelAtMousePosition(mouse);
        }
        
        if (_mouseService.IsScroll(mouse))
        {
            var scrollDelta = _mouseService.GetScrollDelta(mouse);
            
            if (keyboard.IsKeyDown(Keys.LeftControl))
            {
                if (scrollDelta > 0)
                {
                    ScrollButtonsLeft();
                }
                else
                {
                    ScrollButtonsRight();
                }
            }
            else
            {
                _cameraService.ChangeZoom(mouse, scrollDelta);
            }
        }

        _menuButton.Update(mouse);
        
        LayoutVisibleButtons();
        foreach (var button in _colorButtons
                     .Skip(_visibleStartIndex)
                     .Take(VisibleButtons))
        {
            button.Update(mouse);
        }
        
        _cameraService.Update(mouse);
        
        _mouseService.SetMouse(mouse);
    }

    #region Update methods

    private void UpdateSelectedButton()
    {
        var clickedButtonIndex = _colorButtons.FindIndex(x => x.IsHovered);

        if (clickedButtonIndex != -1)
        {
            SelectButton(clickedButtonIndex);
        }
    }
    
    private void ScrollButtonsLeft()
    {
        _visibleStartIndex--;

        if (_visibleStartIndex < 0)
            _visibleStartIndex = 0;
    }

    private void ScrollButtonsRight()
    {
        var max = Math.Max(0, _colorButtons.Count - VisibleButtons);

        _visibleStartIndex++;

        if (_visibleStartIndex > max)
            _visibleStartIndex = max;
    }
    
    private void LayoutVisibleButtons()
    {
        int x = _buttonSpacing;
        int y = _graphicsDevice.Viewport.Height - _buttonSize - _buttonSpacing;

        foreach (var button in _colorButtons)
        {
            button.Bounds = Rectangle.Empty;
        }

        for (int i = 0; i < VisibleButtons; i++)
        {
            int index = _visibleStartIndex + i;

            if (index >= _colorButtons.Count)
                break;

            _colorButtons[index].Bounds = new Rectangle(
                x,
                y,
                _buttonSize,
                _buttonSize);

            x += _buttonSize + _buttonSpacing;
        }
    }
    
    private void SelectButton(int index)
    {
        foreach (var button in _colorButtons)
        {
            button.SetSelected(false);
        }

        _colorButtons[index].SetSelected(true);
    }
    
    private void PaintPixelAtMousePosition(MouseState mouse)
    {
        var selectedButton = _colorButtons.FirstOrDefault(x => x.IsSelected);
        
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
        if (_colorButtons.Any(x => x.IsHovered))
        {
            return true;
        }

        if (_menuButton.IsHovered)
        {
            return true;
        }

        return false;
    }

    #endregion

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
        DrawColorButtons(colorGroups);
        
        _menuButton.Draw(_spriteBatch);

        _spriteBatch.End();
    }

    #region Draw methods
    
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
                    pixel.ColorIsDark() ? Color.White : Color.Black,
                    _cameraService.Zoom);
            }
        }
    }
    
    private void DrawColorButtons(Dictionary<Color, PixelColorGroup> colorGroups)
    {
        foreach (var colorButton in _colorButtons.Skip(_visibleStartIndex).Take(VisibleButtons))
        {
            colorButton.Draw(_spriteBatch, _pixelTexture);

            var colorGroup = colorGroups[colorButton.Color];
            var groupIsFinished = colorGroup.IsFinished;

            var text = groupIsFinished ? "x" : colorButton.Number.ToString();

            _drawService.DrawString(
                _spriteBatch,
                text,
                colorButton.GetDrawBounds().Center.ToVector2(),
                colorButton.ColorIsDark() ? Color.White : Color.Black);

            if (!groupIsFinished)
            {
                _drawService.DrawProgressBar(
                    _spriteBatch,
                    _pixelTexture,
                    colorButton.GetProgressBounds(),
                    colorGroup.Progress,
                    Color.White,
                    Color.White,
                    colorButton.Color);
            }
        }
    }
    
    #endregion

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
    
    private void CreateColorButtons()
    {
        foreach (var group in _processorService.GetPixelColorGroups().Values)
        {
            _colorButtons.Add(new ColorButton(
                group.OriginalColor,
                group.Number,
                Rectangle.Empty));
        }

        LayoutVisibleButtons();
        SelectButton(0);
    }
}