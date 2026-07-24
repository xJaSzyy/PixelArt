using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelArt.Buttons;

public class ColorButton(Color color, int number, Rectangle bounds)
{
    public Color Color { get; } = color;
    public int Number { get; } = number;
    private Rectangle Bounds { get; set; } = bounds;
    public bool IsHovered { get; set; }
    public bool IsSelected { get; set; }
    
    public void Update(MouseState mouse)
    {
        IsHovered = Bounds.Contains(mouse.Position);
    }
    
    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        var rect = GetDrawBounds();

        if (IsSelected)
        {
            var border = new Rectangle(
                rect.X - 2,
                rect.Y - 2,
                rect.Width + 4,
                rect.Height + 4
            );

            spriteBatch.Draw(pixelTexture, border, Color.Yellow);
        }

        var color = Color;

        if (IsHovered)
        {
            color.A = 200;
        }

        spriteBatch.Draw(pixelTexture, rect, color);
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