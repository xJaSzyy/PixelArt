using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;

namespace PixelArt.Services;

public class ColorButtonsService(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, PixelProcessorService processorService)
{
    private readonly List<ColorButton> _colorButtons = [];
    private int _visibleStartIndex;

    private int VisibleButtons => Math.Max(1,
        (graphicsDevice.Viewport.Width - _buttonSpacing * 2) / (_buttonSize + _buttonSpacing));

    private const int _buttonSize = 56;
    private const int _buttonSpacing = 12;

    private Texture2D _pixelTexture;

    private void CreateColorButtons()
    {
        foreach (var group in processorService.GetPixelColorGroups().Values.OrderBy(x => x.Number))
        {
            _colorButtons.Add(new ColorButton(
                group.OriginalColor,
                group.Number,
                Rectangle.Empty));
        }

        LayoutVisibleButtons();
        SelectButton(0);
    }

    public void LoadContent(Texture2D pixelTexture)
    {
        _pixelTexture = pixelTexture;
        CreateColorButtons();
    }

    public void Update(MouseState mouse)
    {
        LayoutVisibleButtons();
        foreach (var button in _colorButtons
                     .Skip(_visibleStartIndex)
                     .Take(VisibleButtons))
        {
            button.Update(mouse);
        }
    }
    
    public void Draw(DrawService drawService)
    {
        var colorGroups = processorService.GetPixelColorGroups();
        
        foreach (var colorButton in _colorButtons.Skip(_visibleStartIndex).Take(VisibleButtons))
        {
            colorButton.Draw(spriteBatch, _pixelTexture);

            var colorGroup = colorGroups[colorButton.Color];
            var groupIsFinished = colorGroup.IsFinished;

            var text = groupIsFinished ? "x" : colorButton.Number.ToString();

            drawService.DrawString(
                spriteBatch,
                text,
                colorButton.GetDrawBounds().Center.ToVector2(),
                colorButton.ColorIsDark() ? Color.White : Color.Black);

            if (!groupIsFinished)
            {
                drawService.DrawProgressBar(
                    spriteBatch,
                    _pixelTexture,
                    colorButton.GetProgressBounds(),
                    colorGroup.Progress,
                    Color.White,
                    Color.White,
                    colorButton.Color);
            }
        }
    }
    
    private void LayoutVisibleButtons()
    {
        var x = _buttonSpacing;
        var y = graphicsDevice.Viewport.Height - _buttonSize - _buttonSpacing;

        foreach (var button in _colorButtons)
        {
            button.Bounds = Rectangle.Empty;
        }

        for (var i = 0; i < VisibleButtons; i++)
        {
            var index = _visibleStartIndex + i;

            if (index >= _colorButtons.Count)
            {
                break;
            }

            _colorButtons[index].Bounds = new Rectangle(x, y,
                _buttonSize, _buttonSize);

            x += _buttonSize + _buttonSpacing;
        }
    }

    public void UpdateSelectedButton()
    {
        var clickedButtonIndex = _colorButtons.FindIndex(x => x.IsHovered);

        if (clickedButtonIndex != -1)
        {
            SelectButton(clickedButtonIndex);
        }
    }
    
    private void SelectButton(int index)
    {
        foreach (var button in _colorButtons)
        {
            button.SetSelected(false);
        }

        _colorButtons[index].SetSelected(true);
        HighlightPixels(index);
    }

    private void HighlightPixels(int colorButtonIndex)
    {
        var highlightColor = new Color(72, 72, 72);

        var groups = processorService.GetPixelColorGroups();

        foreach (var group in groups.Values)
        {
            foreach (var pixel in group.Pixels.Where(pixel => pixel.CurrentColor == highlightColor))
            {
                processorService.SetPixel(
                    processorService.GetPixelIndex(pixel),
                    pixel.GrayColor
                );
            }
        }

        var selectedColor = _colorButtons[colorButtonIndex].Color;

        if (!groups.TryGetValue(selectedColor, out var selectedGroup))
        {
            return;
        }

        foreach (var pixel in selectedGroup.Pixels)
        {
            processorService.SetPixel(
                processorService.GetPixelIndex(pixel),
                highlightColor
            );
        }
    }

    public void ScrollButtonsLeft()
    {
        _visibleStartIndex--;

        if (_visibleStartIndex < 0)
        {
            _visibleStartIndex = 0;
        }
    }

    public void ScrollButtonsRight()
    {
        var max = Math.Max(0, _colorButtons.Count - VisibleButtons);

        _visibleStartIndex++;

        if (_visibleStartIndex > max)
        {
            _visibleStartIndex = max;
        }
    }

    public List<ColorButton> GetButtons()
    {
        return _colorButtons;
    }
}