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
    private float _scroll;
    private float _targetScroll;

    private const float _scrollSpeed = 0.2f;
    private const int _buttonSize = 68;
    private const int _buttonSpacing = 4;
    private readonly Color _highlightColor = new(72, 72, 72);
    
    private Texture2D _pixelTexture;

    private void CreateColorButtons()
    {
        foreach (var group in processorService.CurrentLevel.ColorGroups.OrderBy(x => x.Number))
        {
            _colorButtons.Add(new ColorButton(
                group.OriginalColor,
                group.Number,
                Rectangle.Empty));
        }

        SelectButton(0);
    }

    public void LoadContent(Texture2D pixelTexture)
    {
        _pixelTexture = pixelTexture;
        CreateColorButtons();
    }

    public void Update(MouseState mouse)
    {
        _scroll = MathHelper.Lerp(_scroll, _targetScroll, _scrollSpeed);

        LayoutButtons();

        foreach (var button in _colorButtons)
        {
            button.Update(mouse);
        }
    }
    
    public void Draw(DrawService drawService)
    {
        var colorGroups = processorService.CurrentLevel.ColorGroups;
        foreach (var colorButton in _colorButtons)
        {
            colorButton.Draw(spriteBatch, _pixelTexture);

            var colorGroup = colorGroups.First(x => x.OriginalColor == colorButton.Color);
            var groupIsFinished = colorGroup.IsFinished;

            var text = groupIsFinished ? "x" : colorButton.Number.ToString();

            var color = colorButton.ColorIsDark() ? Color.White : Color.Black;
            
            drawService.DrawString(
                spriteBatch,
                text,
                colorButton.Bounds.Center.ToVector2(),
                color,
                colorButton.GetNumberScale());

            if (!groupIsFinished && colorButton.IsSelected)
            {
                drawService.DrawProgressBar(
                    spriteBatch,
                    _pixelTexture,
                    colorButton.GetProgressBounds(),
                    colorGroup.Progress,
                    color,
                    color,
                    colorButton.Color);
            }
        }
    }
    
    private void LayoutButtons()
    {
        var x = _buttonSpacing - (int)_scroll;
        var y = graphicsDevice.Viewport.Height - _buttonSize - _buttonSpacing;

        foreach (var button in _colorButtons)
        {
            button.Bounds = new Rectangle(
                x,
                y,
                _buttonSize,
                _buttonSize);

            x += _buttonSize + _buttonSpacing;
        }
    }

    public void UpdateSelectedButton()
    {
        var clickedButtonIndex = _colorButtons.FindIndex(x => x.IsHovered);

        if (clickedButtonIndex == -1)
        {
            return;
        }
        
        var colorGroup = processorService.CurrentLevel.ColorGroups.First(x => x.OriginalColor == _colorButtons[clickedButtonIndex].Color);
        if (!colorGroup.IsFinished)
        {
            SelectButton(clickedButtonIndex);
        }
    }

    public void SelectButton(int index)
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
        var changes = ClearHighlight(false);

        var selectedGroup = processorService.CurrentLevel.ColorGroups
            .FirstOrDefault(x => x.OriginalColor == _colorButtons[colorButtonIndex].Color);
        if (selectedGroup == null)
        {
            return;
        }

        foreach (var pixel in selectedGroup.Pixels)
        {
            changes.Add((
                processorService.GetPixelIndex(pixel),
                _highlightColor
            ));
        }
        
        processorService.SetPixels(changes);
    }

    public List<(int Index, Color Color)> ClearHighlight(bool applyChanges = false)
    {
        var changes = new List<(int Index, Color Color)>();
        
        foreach (var group in processorService.CurrentLevel.ColorGroups)
        {
            foreach (var pixel in group.Pixels.Where(p => p.CurrentColor == _highlightColor))
            {
                changes.Add((
                    processorService.GetPixelIndex(pixel),
                    pixel.GrayColor
                ));
            }
        }

        if (applyChanges)
        {
            processorService.SetPixels(changes);
        }

        return changes;
    }

    public void ScrollButtonsLeft()
    {
        _targetScroll -= _buttonSize + _buttonSpacing;

        if (_targetScroll < 0)
        {
            _targetScroll = 0;
        }
    }
    
    public void ScrollButtonsRight()
    {
        var max = Math.Max(0, _colorButtons.Count * (_buttonSize + _buttonSpacing) - graphicsDevice.Viewport.Width + _buttonSpacing * 2);

        _targetScroll += _buttonSize + _buttonSpacing;

        if (_targetScroll > max)
        {
            _targetScroll = max;
        }
    }

    public List<ColorButton> GetButtons()
    {
        return _colorButtons;
    }
}