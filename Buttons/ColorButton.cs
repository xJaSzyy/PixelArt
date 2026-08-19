using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelArt.Buttons;

public class ColorButton(Color color, int number, Rectangle bounds)
{
    public Color Color { get; } = color;
    public int Number { get; } = number;
    public Rectangle Bounds { get; set; } = bounds;
    public bool IsHovered { get; set; }
    public bool IsSelected { get; set; }
    
    public void Update(MouseState mouse)
    {
        IsHovered = Bounds.Contains(mouse.Position);
    }
    
    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        var rect = GetDrawBounds();

        var color = Color;

        if (IsHovered)
        {
            color = new Color(
                Math.Min(color.R + 40, 255),
                Math.Min(color.G + 40, 255),
                Math.Min(color.B + 40, 255)
            );
        }

        spriteBatch.Draw(pixelTexture, rect, color);

        if (IsSelected)
        {
            const int borderSize = 3;
            var selectedColor = ColorIsDark() ? Color.Yellow : Color.CornflowerBlue;

            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(rect.X - borderSize, rect.Y - borderSize,
                    rect.Width + borderSize * 2, borderSize),
                selectedColor);

            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(rect.X - borderSize, rect.Bottom,
                    rect.Width + borderSize * 2, borderSize),
                selectedColor);

            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(rect.X - borderSize, rect.Y,
                    borderSize, rect.Height),
                selectedColor);

            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(rect.Right, rect.Y,
                    borderSize, rect.Height),
                selectedColor);
        }
    }
    
    public Rectangle GetDrawBounds()
    {
        var rect = Bounds;

        if (IsHovered)
        {
            rect.Y -= 4;
        }

        return rect;
    }
    
    public Rectangle GetProgressBounds()
    {
        var rect = GetDrawBounds();
        
        const int height = 8;

        return new Rectangle(
            rect.X,
            rect.Bottom - height,
            rect.Width,
            height);
    }
    
    public bool ColorIsDark()
    {
        var brightness =
            0.299f * Color.R +
            0.587f * Color.G +
            0.114f * Color.B;

        return brightness < 128;
    }

    public void SetSelected(bool isSelected)
    {
        IsSelected = isSelected;
    }
}