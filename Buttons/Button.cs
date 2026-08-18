using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelArt.Buttons;

public class Button(Texture2D texture, Rectangle bounds)
{
    private Texture2D Texture { get; set; } = texture;
    public Rectangle Bounds { get; set; } = bounds;

    public bool IsHovered { get; set; }

    public void Update(MouseState mouse)
    {
        IsHovered = Bounds.Contains(mouse.Position);
    }

    public void Draw(SpriteBatch spriteBatch, Color? color = null)
    {
        var rect = Bounds;

        color ??= Color.White;
        
        if (IsHovered)
        {
            rect.Y -= 4;
        }

        spriteBatch.Draw(Texture, rect, (Color)color);
    }
}