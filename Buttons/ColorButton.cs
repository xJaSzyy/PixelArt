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
    
    private float NumberScale { get; } = 1.1f;
    
    public void Update(MouseState mouse)
    {
        IsHovered = Bounds.Contains(mouse.Position);
    }
    
    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        var color = Color;

        if (IsHovered)
        {
            var l = ColorIsDark() ? 40 : -40;
            
            color = new Color(
                Math.Min(color.R + l, 255),
                Math.Min(color.G + l, 255),
                Math.Min(color.B + l, 255)
            );
        }

        spriteBatch.Draw(pixelTexture, Bounds, color);
    }
    
    public Rectangle GetProgressBounds()
    {
        const int spacing = 8;

        return new Rectangle(
            Bounds.X + spacing,
            Bounds.Bottom - spacing * 2,
            Bounds.Width - spacing * 2,
            spacing);
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

    public float GetNumberScale()
    {
        return IsSelected ? NumberScale * 1.2f : IsHovered ? NumberScale * 1.1f : NumberScale;
    }
}